using Backend.DTOs;

namespace Backend.Interfaces
{
    public interface IInterviewService
    {
        Task<StartInterviewResponse> StartInterview(string role, string language = "en");
        Task<SubmitAnswerResponse> SubmitAnswerAsync(string sessionId, string answer);
        Task<SubmitAnswerResponse> EndInterviewEarlyAsync(string sessionId);
    }
}