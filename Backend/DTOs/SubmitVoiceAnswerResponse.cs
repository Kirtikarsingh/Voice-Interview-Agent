using Backend.Models;

namespace Backend.DTOs
{
    public class SubmitVoiceAnswerResponse
    {
        public required string SessionId { get; set; }
        public required string TranscribedAnswer { get; set; }
        public required string Feedback { get; set; }
        public bool IsFollowUp { get; set; }
        public string? NextQuestion { get; set; }
        public bool Completed { get; set; }
        public string? Error { get; set; }
        public InterviewFeedback? FinalFeedback { get; set; }
    }
}