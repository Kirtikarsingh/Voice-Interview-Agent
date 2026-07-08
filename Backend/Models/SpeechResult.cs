namespace Backend.Models
{
    public class SpeechResult
    {
        public bool Success { get; set; }
        public byte[]? AudioBytes { get; set; }
        public string? Error { get; set; }
    }
}