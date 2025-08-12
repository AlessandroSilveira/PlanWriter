using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int TotalWordsGoal { get; set; }
        public int CurrentWordCount { get; set; }
        public TimeSpan TotalTimeSpent { get; set; } = TimeSpan.Zero;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int RemainingWords => TotalWordsGoal - CurrentWordCount;

        public double CompletionPercentage => TotalWordsGoal == 0
            ? 0
            : (double)CurrentWordCount / TotalWordsGoal * 100;

        public ICollection<ProjectProgressEntry> ProgressEntries { get; set; } = new List<ProjectProgressEntry>();
    }
}