using System;

namespace PlanWriter.Application.DTO
{
    public class AddProjectProgressDto
    {
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Total de palavras escritas no progresso.
        /// </summary>
        public int TotalWordsWritten { get; set; }

        /// <summary>
        /// Tempo gasto em minutos.
        /// </summary>
        public int TimeSpentInMinutes { get; set; }

        /// <summary>
        /// Data do progresso. Se não fornecida, será assumida como hoje (UTC).
        /// </summary>
        public DateTime? Date { get; set; }
    }
}