using Backend.Models;

namespace Backend.Interfaces
{
    public interface IRetrievalService
    {
        List<InterviewQuestion> GetQuestionsByRole(string role);
        InterviewQuestion? GetQuestionById(string role, int id);
    }
}