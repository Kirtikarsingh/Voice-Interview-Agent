namespace Backend.Models
{
    public class InterviewSession
    {
        public required string SessionId { get; set; }
        public required string Role { get; set; }
        public required List<InterviewQuestion> Questions { get; set; }
        public int CurrentQuestionIndex { get; set; } = 0;
        public bool AwaitingFollowUp { get; set; } = false;
        public int FollowUpIndexUsed { get; set; } = -1;
        public List<InterviewExchange> Transcript { get; set; } = new();
        public bool IsComplete { get; set; } = false;
        public string Language { get; set; } = "en";
    }

    public class InterviewExchange
    {
        public required string Question { get; set; }
        public required string Answer { get; set; }
        public required string IdealAnswer { get; set; }
        public required string Feedback { get; set; }
        public bool IsAcceptable { get; set; }
    }
}