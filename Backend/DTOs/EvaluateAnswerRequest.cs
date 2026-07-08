namespace Backend.DTOs
{
    public class EvaluateAnswerRequest
    {
        public required string Role { get; set; }
        public required int QuestionId { get; set; }
        public required string CandidateAnswer { get; set; }
    }
}