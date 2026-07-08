namespace Backend.DTOs
{
    public class SubmitAnswerRequest
    {
        public required string SessionId { get; set; }
        public required string Answer { get; set; }
    }
}