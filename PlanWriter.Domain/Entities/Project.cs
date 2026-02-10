using System;
using System.Collections.Generic;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
        public string? Title { get; set; }
        public string? Genre { get; set; }
        public string? Description { get; set; }
        
        public int GoalAmount { get; set; }           
        public GoalUnit GoalUnit { get; set; } = GoalUnit.Words;
        public int? WordCountGoal { get; set; }        
    
        public DateTime CreatedAt { get; set; }
        public byte[]? CoverBytes { get; set; }      
        public string? CoverMime { get; set; }       
        public int? CoverSize { get; set; }          
        public DateTime? CoverUpdatedAt { get; set; }
        public ICollection<ProjectProgress> ProgressEntries { get; set; } = new List<ProjectProgress>();
        public DateTime StartDate { get; set; }   
        public DateTime? Deadline { get; set; }
        public int CurrentWordCount { get; set; }
        public bool IsPublic { get; set; }
        public int? ValidatedWords { get; set; }
        public DateTime? ValidatedAtUtc { get; set; }
        public bool? ValidationPassed { get; set; }
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    }
}