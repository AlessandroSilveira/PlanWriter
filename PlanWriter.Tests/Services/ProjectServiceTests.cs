using Moq;
using PlanWriter.Application.DTO;
using PlanWriter.Application.DTOs;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Services;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;
using Assert = Xunit.Assert;

namespace PlanWriter.Tests.Services;

    public class ProjectServiceTests
    {
        private readonly Mock<IProjectRepository> _repositoryMock;
        private readonly IProjectService _service;

        public ProjectServiceTests()
        {
            _repositoryMock = new Mock<IProjectRepository>();
            _service = new ProjectService(_repositoryMock.Object);
        }

         [Fact(DisplayName = "Deve lançar exceção se o projeto não for encontrado")]
        public async Task AddProgressAsync_ThrowsException_WhenProjectNotFound()
        {
            // Arrange
            var dto = new AddProjectProgressDto
            {
                ProjectId = Guid.NewGuid(),
                TotalWordsWritten = 1200
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(dto.ProjectId))
                           .ReturnsAsync((Project?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.AddProgressAsync(dto));
            Assert.Equal("Project not found.", ex.Message);

            _repositoryMock.Verify(r => r.GetByIdAsync(dto.ProjectId), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
            _repositoryMock.Verify(r => r.AddProgressEntryAsync(It.IsAny<ProjectProgress>()), Times.Never);
        }

        [Fact(DisplayName = "Deve atualizar o projeto e salvar o progresso quando válido")]
        public async Task AddProgressAsync_UpdatesProject_AndSavesProgressEntry_WhenProjectExists()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project
            {
                Id = projectId,
                Name = "My Book",
                TotalWordsGoal = 10000,
                CurrentWordCount = 2000
            };

            var dto = new AddProjectProgressDto
            {
                ProjectId = projectId,
                TotalWordsWritten = 4000
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(projectId))
                           .ReturnsAsync(project);

            // Act
            await _service.AddProgressAsync(dto);

            // Assert
            _repositoryMock.Verify(r => r.GetByIdAsync(projectId), Times.Once);

            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Project>(p =>
                p.Id == projectId &&
                p.CurrentWordCount == dto.TotalWordsWritten
            )), Times.Once);

            _repositoryMock.Verify(r => r.AddProgressEntryAsync(It.Is<ProjectProgress>(entry =>
                entry.ProjectId == projectId &&
                entry.TotalWordsWritten == dto.TotalWordsWritten &&
                entry.RemainingWords == (project.TotalWordsGoal - dto.TotalWordsWritten) &&
                Math.Abs(entry.RemainingPercentage - (100 - ((double)dto.TotalWordsWritten / project.TotalWordsGoal * 100))) < 0.01
            )), Times.Once);
        }
    }
