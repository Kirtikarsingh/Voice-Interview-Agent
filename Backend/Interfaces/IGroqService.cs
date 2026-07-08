using Backend.Models;

namespace Backend.Interfaces
{
    public class AnswerEvaluationResult
    {
        public bool IsAcceptable { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public bool NeedsFollowUp { get; set; }
    }

    public interface IGroqService
    {
        Task<AnswerEvaluationResult> EvaluateAnswerAsync(
            string question,
            string idealAnswer,
            List<string> keywords,
            string candidateAnswer,
            string language = "en");

        Task<InterviewFeedback> GenerateFinalFeedbackAsync(
            string role,
            List<InterviewExchange> transcript,
            string language = "en");

        Task<string> TranslateTextAsync(string text, string targetLanguage);
    }
}