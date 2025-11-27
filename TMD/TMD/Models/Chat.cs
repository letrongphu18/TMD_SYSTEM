namespace TMD.Models
{
    public class Chat
    {
        public int Id { get; set; } // hoặc Guid nếu dùng UUID/GUID
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public string? MessageText { get; set; }
        public string? AttachmentUrl { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
