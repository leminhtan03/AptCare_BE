using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.Webhooks;

namespace AptCare.Service.Services.Implements.PayOSService
{
    public class PayOSWebhookService : IPayOSWebhookService
    {
        private readonly IUnitOfWork<AptCareSystemDBContext> _uow;
        private readonly PayOSClient _payOS;
        private readonly PayOSOptions _payOSOptions;
        private readonly ILogger<PayOSWebhookService> _logger;

        public PayOSWebhookService(IUnitOfWork<AptCareSystemDBContext> uow, ILogger<PayOSWebhookService> logger, IOptions<PayOSOptions> payOSOptions)
        {
            _uow = uow;
            _payOSOptions = payOSOptions.Value;
            _payOS = new PayOSClient(
                 _payOSOptions.ClientId,
                 _payOSOptions.ApiKey,
                 _payOSOptions.ChecksumKey
             );
            _logger = logger;
        }

        public async Task HandleAsync(Webhook webhookData)
        {
            try
            {
                var isValidSignature = _payOS.Webhooks.VerifyAsync(webhookData);
                if (isValidSignature != null)
                {
                    _logger.LogWarning("Invalid PayOS webhook signature for orderCode {OrderCode}",
                        webhookData.Data.OrderCode);
                    return;
                }

                _logger.LogInformation("Valid PayOS webhook signature for orderCode {OrderCode}",
                    webhookData.Data.OrderCode);

                await _uow.BeginTransactionAsync();
                var txRepo = _uow.GetRepository<Transaction>();

                var tx = await txRepo.SingleOrDefaultAsync(
                    predicate: t => t.Provider == PaymentProvider.PayOS
                        && t.OrderCode == webhookData.Data.OrderCode,
                    include: i => i.Include(t => t.Invoice)
                );

                if (tx == null)
                {
                    _logger.LogWarning("Transaction not found for orderCode {OrderCode}",
                        webhookData.Data.OrderCode);
                    return;
                }

                _logger.LogInformation("Found transaction {TxId} for orderCode {OrderCode}",
                    tx.TransactionId, webhookData.Data.OrderCode);

                if (tx.Status == TransactionStatus.Success)
                {
                    _logger.LogInformation("Transaction {TxId} already Success - no action needed",
                        tx.TransactionId);
                    return;
                }

                await ProcessPaymentStatusAsync(tx, webhookData);
                await _uow.CommitTransactionAsync();

                _logger.LogInformation("Successfully processed PayOS webhook for transaction {TxId}",
                    tx.TransactionId);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing PayOS webhook for orderCode {OrderCode}",
                    webhookData.Data.OrderCode);
                throw;
            }
        }

        private async Task ProcessPaymentStatusAsync(Transaction tx, Webhook webhookData)
        {
            var txRepo = _uow.GetRepository<Transaction>();
            var invoiceRepo = _uow.GetRepository<Invoice>();

            switch (webhookData.Code)
            {
                case "00":
                    _logger.LogInformation("Processing PAID status for transaction {TxId}", tx.TransactionId);
                    tx.Status = TransactionStatus.Success;
                    tx.PayOSTransactionId = webhookData.Data.PaymentLinkId;
                    tx.PaidAt = DateTime.Now;
                    txRepo.UpdateAsync(tx);

                    tx.Invoice.Status = InvoiceStatus.Paid;
                    invoiceRepo.UpdateAsync(tx.Invoice);

                    _logger.LogInformation("Transaction {TxId} marked as Success", tx.TransactionId);
                    break;

                case "01":
                    _logger.LogInformation("Processing CANCELLED status for transaction {TxId}",
                        tx.TransactionId);
                    tx.Status = TransactionStatus.Cancelled;
                    tx.PayOSTransactionId = webhookData.Data.PaymentLinkId;
                    txRepo.UpdateAsync(tx);

                    _logger.LogInformation("Transaction {TxId} marked as Cancelled", tx.TransactionId);
                    break;

                default:
                    _logger.LogWarning("Unknown PayOS webhook code '{Code}' for transaction {TxId}",
                        webhookData.Code, tx.TransactionId);
                    break;
            }

            await _uow.CommitAsync();
        }
    }
}