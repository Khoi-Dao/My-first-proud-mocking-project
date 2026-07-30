using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SmartParking.BusinessObject.Shared.Models.KhoiDN.Events;
using SmartParking.Notification.SignalRService.KhoiDN.Hubs;

namespace SmartParking.Notification.SignalRService.KhoiDN.Consumers
{
    // Lắng nghe sự kiện thanh toán thành công từ RabbitMQ
    public class TransactionCompletedConsumer : IConsumer<TransactionCompletedEvent>
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<TransactionCompletedConsumer> _logger;

        // Inject SignalR HubContext để có thể bắn thông báo ra cho các Client đang kết nối
        public TransactionCompletedConsumer(IHubContext<NotificationHub> hubContext, ILogger<TransactionCompletedConsumer> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TransactionCompletedEvent> context)
        {
            var txEvent = context.Message;
            _logger.LogInformation("Nhận được sự kiện giao dịch hoàn tất cho Hóa đơn ID: {InvoiceId}, Mã TX: {Code}", txEvent.InvoiceId, txEvent.TransactionCode);

            // Bắn thông báo thời gian thực qua SignalR với tên sự kiện là "ReceiveTransactionNotification"
            // Toàn bộ các máy Client (MAUI/Blazor) đang mở màn hình sẽ nhận được gói JSON này tức thì
            await _hubContext.Clients.All.SendAsync("ReceiveTransactionNotification", new
            {
                InvoiceId = txEvent.InvoiceId,
                TransactionCode = txEvent.TransactionCode,
                Amount = txEvent.Amount,
                Status = txEvent.PaymentStatus,
                Time = txEvent.CompletedAt,
                Message = $"Giao dịch {txEvent.TransactionCode} trị giá {txEvent.Amount:N0} VND đã xử lý ({txEvent.PaymentStatus})!"
            });
        }
    }
}
