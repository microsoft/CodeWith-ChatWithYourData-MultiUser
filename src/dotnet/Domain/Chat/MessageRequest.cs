namespace Domain.Chat
{
    public record MessageRequest
    {
        public required string Message { get; set; }
        public List<string>DocumentIds { get; set; } = new List<string>();
        public List<string> Tools { get; set; } = new List<string>();
    }
}
