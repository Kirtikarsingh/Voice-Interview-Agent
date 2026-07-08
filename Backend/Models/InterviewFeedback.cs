namespace Backend.Models
{
    public class QuestionBreakdown
    {
        public string Question { get; set; } = string.Empty;
        public string CandidateAnswer { get; set; } = string.Empty;
        public string IdealAnswer { get; set; } = string.Empty;
        public int TechnicalAccuracy { get; set; } // 0-100
        public int Completeness { get; set; }      // 0-100
        public int Clarity { get; set; }            // 0-100
        public string Comment { get; set; } = string.Empty;
    }

    public class InterviewFeedback
    {
        public int OverallScore { get; set; } // out of 100
        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> AreasToImprove { get; set; } = new();
        public List<QuestionBreakdown> QuestionBreakdown { get; set; } = new();
    }
}