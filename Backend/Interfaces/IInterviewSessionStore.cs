using Backend.Models;

namespace Backend.Interfaces
{
    public interface IInterviewSessionStore
    {
        void CreateSession(InterviewSession session);
        InterviewSession? GetSession(string sessionId);
    }
}