using Google;
using TMD.Models;
using Microsoft.EntityFrameworkCore;

namespace TMD.Services
{
    public class ChatService
    {
        private readonly TmdContext _context;

        public ChatService(TmdContext context)
        {
            _context = context;
        }

        public async Task<bool> SaveMessageAsync(Chat message)
        {
            try
            {
                _context.Chats.Add(message);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ SaveMessageAsync error: " + ex.Message);
                return false;
            }
        }

        //  Lấy lịch sử chat
        public async Task<List<Chat>> GetConversationHistory(string userId1, string userId2)
        {
            return await _context.Chats
                .Where(c => (c.SenderId == userId1 && c.ReceiverId == userId2) ||
                            (c.SenderId == userId2 && c.ReceiverId == userId1))
                .OrderBy(c => c.Timestamp)
                .ToListAsync();
        }

        //  Đánh dấu đã đọc
        public async Task<int> MarkMessagesAsRead(string readerId, string senderId)
        {
            var messages = await _context.Chats
                .Where(c => c.SenderId == senderId
                         && c.ReceiverId == readerId
                         && !c.IsRead)
                .ToListAsync();

            messages.ForEach(m => m.IsRead = true);

            await _context.SaveChangesAsync();

            return messages.Count;
        }


    }
}
