using MassTransit;
using SU26_PRN232_Payment.Services.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN.DTOs.Transaction; // Gọi đúng namespace DTO mà Interface của bạn đang dùng
using SmartParking.BusinessObject.Shared.Models.KhoiDN.Events;

namespace SmartParking.Transactions.Microservice.KhoiDN.Consumers
{
    // Ngồi rình tin nhắn InvoiceCreatedEvent từ RabbitMQ
    public class TransactionsKhoiDnConsumer : IConsumer<InvoiceCreatedEvent>
    {
        private readonly ITransactionsKhoiDnService _transactionService;
        private readonly IPublishEndpoint _publishEndpoint;

        public TransactionsKhoiDnConsumer(ITransactionsKhoiDnService transactionService, IPublishEndpoint publishEndpoint)
        {
            _transactionService = transactionService;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<InvoiceCreatedEvent> context)
        {
            var invoiceEvent = context.Message;

            // 1. Khởi tạo DTO giao dịch mới theo đúng yêu cầu validation của bạn
            var newTxDto = new TransactionCreateRequestDto
            {
                InvoiceKhoiDnid = invoiceEvent.InvoiceId,
                Amount = invoiceEvent.TotalAmount,
                PaymentMethod = "MOMO", // Chấp nhận: CASH, VNMB, MOMO, BANK
                ReferenceCode = $"REF-{invoiceEvent.InvoiceCode}"
            };

            // 2. Gọi đúng hàm ProcessTransactionAsync trong ITransactionsKhoiDnService
            var createdTx = await _transactionService.ProcessTransactionAsync(newTxDto);

            // 3. Bắn tiếp tin nhắn kết quả lên RabbitMQ để SignalR / MAUI Client cập nhật thời gian thực
            if (createdTx != null)
            {
                await _publishEndpoint.Publish(new TransactionCompletedEvent
                {
                    TransactionId = createdTx.InvoiceKhoiDnId, // Dùng ID hóa đơn làm liên kết vì DTO không có TransactionId riêng
                    TransactionCode = createdTx.TransactionCode ?? string.Empty,
                    InvoiceId = invoiceEvent.InvoiceId,
                    Amount = createdTx.Amount,
                    PaymentStatus = createdTx.PaymentStatus ?? "SUCCESS",
                    CompletedAt = createdTx.TransactionTime
                });
            }
        }
    }
}