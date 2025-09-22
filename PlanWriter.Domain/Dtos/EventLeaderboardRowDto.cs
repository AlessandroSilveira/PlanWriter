// Application/Events/Dtos/EventLeaderboardRowDto.cs

using System;

namespace PlanWriter.Domain.Dtos
{
    public class EventLeaderboardRowDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectTitle { get; set; } = "";
        public string UserName { get; set; } = "";
        public int Words { get; set; }
        public double Percent { get; set; } // 0..100 relativo à meta do projeto no evento
        public bool Won { get; set; }
        public int Rank { get; set; }
    }
}