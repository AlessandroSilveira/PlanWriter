using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public string Description { get; set; }
        public int? WordCountGoal { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public byte[]? CoverBytes { get; set; }      // varbinary(max)
        public string? CoverMime { get; set; }       // image/jpeg, image/png...
        public int? CoverSize { get; set; }          // opcional (bytes)
        public DateTime? CoverUpdatedAt { get; set; }// p/ cache e auditoria
       

        // 🔹 Navegação para progresso
        public ICollection<ProjectProgress> ProgressEntries { get; set; } = new List<ProjectProgress>();
        public DateTime? Deadline { get; set; }
        public int CurrentWordCount { get; set; }
        
        public bool IsPublic { get; set; }
    }
}