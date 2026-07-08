namespace Backend.Models
{
    public class TranscriptionResult
    {
        public bool Success { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}