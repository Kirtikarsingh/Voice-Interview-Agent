using Backend.Interfaces;
using Backend.Models;
using Newtonsoft.Json;

namespace Backend.Services
{
    public class RetrievalService : IRetrievalService
    {
        private readonly string _datasetFolder;

        public RetrievalService(IWebHostEnvironment env)
        {
            // Points to: Backend/Data/Datasets
            _datasetFolder = Path.Combine(env.ContentRootPath, "Data", "Datasets");
        }

        public List<InterviewQuestion> GetQuestionsByRole(string role)
        {
            var filePath = Path.Combine(_datasetFolder, $"{role}.json");

            if (!File.Exists(filePath))
            {
                return new List<InterviewQuestion>();
            }

            var json = File.ReadAllText(filePath);
            var questions = JsonConvert.DeserializeObject<List<InterviewQuestion>>(json);

            return questions ?? new List<InterviewQuestion>();
        }

        public InterviewQuestion? GetQuestionById(string role, int id)
        {
            var questions = GetQuestionsByRole(role);
            return questions.FirstOrDefault(q => q.Id == id);
        }
    }
}