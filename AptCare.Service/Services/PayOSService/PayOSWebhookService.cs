
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.PayOSDto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AptCare.Service.Services.PayOSService
{
    public class PayOSWebhookService : IPayOSWebhookService
    {
        private readonly IUnitOfWork<AptCareSystemDBContext> _uow;
        private readonly IPayOSClient _payOSClient;
        private readonly ILogger<PayOSWebhookService> _logger;

        public PayOSWebhookService(IUnitOfWork<AptCareSystemDBContext> uow, IPayOSClient payOSClient, ILogger<PayOSWebhookService> logger)
        {
            _uow = uow;
            _payOSClient = payOSClient;
            _logger = logger;
        }

        public async Task HandleAsync(PayOSWebhookRequest req)
        {
            var sorted = BuildSortedJson(req);
            if (!_payOSClient.VerifySignature(sorted, req.signature))
            {
                _logger.LogWarning("Invalid PayOS signature for orderCode {OrderCode}", req.data.orderCode);
                return;
            }
            var txRepo = _uow.GetRepository<Transaction>();
            var tx = await txRepo.SingleOrDefaultAsync(predicate: t =>
                t.Provider == PaymentProvider.PayOS && t.PayOSOrderCode == req.data.orderCode);

            if (tx == null)
            {
                _logger.LogWarning("Transaction not found for orderCode {OrderCode}", req.data.orderCode);
                return;
            }

            if (tx.Status == TransactionStatus.Success)
            {
                _logger.LogInformation("Transaction {TxId} already Success", tx.TransactionId);
                return;
            }

            if (req.data.status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            {
                tx.Status = TransactionStatus.Success;
                tx.PayOSTransactionId = req.data.transactionId;
                tx.PaidAt = DateTime.UtcNow;
                txRepo.UpdateAsync(tx);
                var invoice = await _uow.GetRepository<Invoice>()
                    .SingleOrDefaultAsync(predicate: i => i.InvoiceId == tx.InvoiceId);
                if (invoice != null)
                {
                    var paid = await txRepo.GetListAsync(
                        predicate: x => x.InvoiceId == invoice.InvoiceId
                            && x.Direction == TransactionDirection.Income
                            && x.Status == TransactionStatus.Success,
                        selector: x => x.Amount);

                    var totalPaid = paid.Sum();

                    if (totalPaid >= invoice.TotalAmount)
                        invoice.Status = InvoiceStatus.Paid;
                    _uow.GetRepository<Invoice>().UpdateAsync(invoice);
                }

                await _uow.CommitAsync();
                _logger.LogInformation("Transaction {TxId} marked as Success (PayOS)", tx.TransactionId);
            }
        }

        private static string BuildSortedJson(PayOSWebhookRequest req)
        {
            var root = new SortedDictionary<string, object?>
            {
                ["code"] = req.code,
                ["data"] = new SortedDictionary<string, object?>
                {
                    ["amount"] = req.data.amount,
                    ["orderCode"] = req.data.orderCode,
                    ["status"] = req.data.status,
                    ["time"] = req.data.time,
                    ["transactionId"] = req.data.transactionId
                },
                ["desc"] = req.desc
            };

            return JsonSerializer.Serialize(root);
        }
    }
}
