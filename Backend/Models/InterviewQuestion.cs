namespace Backend.Models
{
    public class InterviewQuestion
    {
        public int Id { get; set; }
        public required string Category { get; set; }
        public required string Difficulty { get; set; }
        public required string Question { get; set; }
        public required string IdealAnswer { get; set; }
        public List<string> Keywords { get; set; } = new();
        public List<string> FollowUps { get; set; } = new();
    }
}