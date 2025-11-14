using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
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

            _logger.LogInformation("Valid PayOS signature for orderCode {OrderCode}", req.data.orderCode);

            try
            {
                await _uow.BeginTransactionAsync();
                var txRepo = _uow.GetRepository<Transaction>();
                var tx = await txRepo.SingleOrDefaultAsync(
                    predicate: t => t.Provider == PaymentProvider.PayOS
                        && t.PayOSOrderCode == req.data.orderCode,
                    include: i => i.Include(t => t.Invoice)
                );
                if (tx == null)
                {
                    _logger.LogWarning("Transaction not found for orderCode {OrderCode}", req.data.orderCode);
                    return;
                }
                _logger.LogInformation("Found transaction {TxId} for orderCode {OrderCode}", tx.TransactionId, req.data.orderCode);
                if (tx.Status == TransactionStatus.Success)
                {
                    _logger.LogInformation("Transaction {TxId} already Success - no action needed", tx.TransactionId);
                    return;
                }
                await ProcessPaymentStatusAsync(tx, req.data);
                await _uow.CommitTransactionAsync();
                _logger.LogInformation("Successfully processed PayOS webhook for transaction {TxId}", tx.TransactionId);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing PayOS webhook for orderCode {OrderCode}", req.data.orderCode);
                throw;
            }
        }

        private async Task ProcessPaymentStatusAsync(Transaction tx, PayOSWebhookData data)
        {
            var txRepo = _uow.GetRepository<Transaction>();
            var invoiceRepo = _uow.GetRepository<Invoice>();
            switch (data.status.ToUpperInvariant())
            {
                case "PAID":
                    _logger.LogInformation("Processing PAID status for transaction {TxId}", tx.TransactionId);
                    tx.Status = TransactionStatus.Success;
                    tx.PayOSTransactionId = data.transactionId;
                    tx.PaidAt = DateTime.UtcNow;
                    txRepo.UpdateAsync(tx);
                    await UpdateInvoiceStatusAsync(tx.Invoice);

                    _logger.LogInformation("Transaction {TxId} marked as Success", tx.TransactionId);
                    break;
                case "CANCELLED":
                    _logger.LogInformation("Processing CANCELLED status for transaction {TxId}", tx.TransactionId);
                    tx.Status = TransactionStatus.Cancelled;
                    tx.PayOSTransactionId = data.transactionId;
                    txRepo.UpdateAsync(tx);

                    _logger.LogInformation("Transaction {TxId} marked as Cancelled", tx.TransactionId);
                    break;
                case "PENDING":
                    _logger.LogInformation("Transaction {TxId} still PENDING", tx.TransactionId);
                    break;

                default:
                    _logger.LogWarning("Unknown PayOS status '{Status}' for transaction {TxId}",
                        data.status, tx.TransactionId);
                    break;
            }
            await _uow.CommitAsync();
        }

        private async Task UpdateInvoiceStatusAsync(Invoice invoice)
        {
            var invoiceRepo = _uow.GetRepository<Invoice>();
            var txRepo = _uow.GetRepository<Transaction>();

            var successfulIncomeTransactions = await txRepo.GetListAsync(
                predicate: x => x.InvoiceId == invoice.InvoiceId
                    && x.Direction == TransactionDirection.Income
                    && x.Status == TransactionStatus.Success
            );
            var totalPaid = successfulIncomeTransactions.Sum(t => t.Amount);
            var originalStatus = invoice.Status;
            if (totalPaid >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
                _logger.LogInformation("Invoice {InvoiceId} fully paid: {TotalPaid}/{TotalAmount}",
                    invoice.InvoiceId, totalPaid, invoice.TotalAmount);
            }
            else if (totalPaid > 0)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
                _logger.LogInformation("Invoice {InvoiceId} partially paid: {TotalPaid}/{TotalAmount}",
                    invoice.InvoiceId, totalPaid, invoice.TotalAmount);
            }
            else
            {
                invoice.Status = InvoiceStatus.AwaitingPayment;
                _logger.LogInformation("Invoice {InvoiceId} reset to AwaitingPayment", invoice.InvoiceId);
            }

            if (originalStatus != invoice.Status)
            {
                invoiceRepo.UpdateAsync(invoice);
                _logger.LogInformation("Updated invoice {InvoiceId} status: {OldStatus} → {NewStatus}",
                    invoice.InvoiceId, originalStatus, invoice.Status);
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