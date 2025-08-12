using System;
using PlanWriter.Application.DTOs;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Interfaces;

namespace PlanWriter.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repository;

        public ProjectService(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateProjectAsync(CreateProjectDto dto)
        {
            var project = new Project
            {
                Name = dto.Name,
                TotalWordsGoal = dto.TotalWordsGoal,
                CurrentWordCount = 0,
                TotalTimeSpent = TimeSpan.Zero
            };

            await _repository.AddAsync(project);
        }

        public async Task AddProgressAsync(AddProjectProgressDto dto)
        {
            var project = await _repository.GetByIdAsync(dto.ProjectId);
            if (project == null)
                throw new Exception("Project not found.");

            // Atualiza o progresso atual do projeto
            project.CurrentWordCount = dto.TotalWordsWritten;

            var remainingWords = project.TotalWordsGoal - dto.TotalWordsWritten;
            var remainingPercentage = project.TotalWordsGoal == 0
                ? 0
                : 100 - ((double)dto.TotalWordsWritten / project.TotalWordsGoal * 100);

            var progressEntry = new ProjectProgressEntry
            {
                ProjectId = project.Id,
                TotalWordsWritten = dto.TotalWordsWritten,
                RemainingWords = remainingWords,
                RemainingPercentage = remainingPercentage,

                TimeSpentInMinutes = dto.TimeSpentInMinutes,
                Date = dto.Date ?? DateTime.UtcNow
            };

            await _repository.AddProgressEntryAsync(progressEntry);
            await _repository.UpdateAsync(project);
        }


    }
}