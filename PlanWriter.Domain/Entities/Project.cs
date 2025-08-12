using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? WordCountGoal { get; set; }
        public DateTime CreatedAt { get; set; }

        // 🔹 Navegação para progresso
        public ICollection<ProjectProgress> ProgressEntries { get; set; } = new List<ProjectProgress>();
        public DateTime? Deadline { get; set; }
        public int CurrentWordCount { get; set; }
    }
}