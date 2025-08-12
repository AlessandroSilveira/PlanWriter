using System;

namespace PlanWriter.Domain.Entities;

    public class ProjectProgressEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

        public int TotalWordsWritten { get; set; }

        public int RemainingWords { get; set; }

        public double RemainingPercentage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int TimeSpentInMinutes{ get; set; }       
            
    }

