namespace Backend.DTOs
{
    public class StartInterviewResponse
    {
        public required string SessionId { get; set; }
        public required string Question { get; set; }
        public required string Category { get; set; }
        public required string Difficulty { get; set; }
    }
}