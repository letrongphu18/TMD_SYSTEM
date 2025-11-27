using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize] // bắt buộc đăng nhập
public class ChatHub : Hub
{
    public async Task SendMessage(string partnerId, string messageText, string attachmentUrl)
    {
        var senderId = Context.User?.Identity?.Name;

        // Gửi cho chính người gửi (để hiện tin nhắn ngay lập tức + trạng thái Đã gửi)
        await Clients.User(senderId).SendAsync("ReceiveMessage", new
        {
            senderId,
            messageText,
            attachmentUrl,
            timestamp = DateTime.Now,
            isRead = false
        });

        // Gửi cho người nhận (Admin hoặc Staff)
        await Clients.User(partnerId).SendAsync("ReceiveMessage", new
        {
            senderId,
            messageText,
            attachmentUrl,
            timestamp = DateTime.Now,
            isRead = false
        });
    }

    public async Task MarkMessagesAsRead(string partnerId)
    {
        var callerId = Context.User?.Identity?.Name;
        await Clients.User(partnerId).SendAsync("MessagesMarkedAsRead", callerId);
    }

    // Tự động thêm user vào group theo Id khi kết nối
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        await base.OnConnectedAsync();
    }
}