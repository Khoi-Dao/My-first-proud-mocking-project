using Microsoft.AspNetCore.SignalR;

namespace SmartParking.Notification.SignalRService.KhoiDN.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Ghi log hoặc thông báo khi có một Client mới kết nối vào Hub
            await Clients.Caller.SendAsync("ReceiveMessage", "Hệ thống", "Đã kết nối thành công đến Trạm thông báo thời gian thực!");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
