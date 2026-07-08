namespace Backend.DTOs
{
    public class StartInterviewRequest
    {
        public required string Role { get; set; }
        public string Language { get; set; } = "en"; // "en", "hi", or "de"
    }
}