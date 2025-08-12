namespace PlanWriter.Application.DTOs
{
    public class CreateProjectDto
    {
        public string Name { get; set; } = string.Empty;
        public int TotalWordsGoal { get; set; }
    }
}