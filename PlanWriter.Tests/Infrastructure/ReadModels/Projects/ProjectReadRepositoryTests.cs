using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Projects;

public class ProjectReadRepositoryTests
{
    [Fact]
    public async Task GetUserProjectsAsync_ShouldReturnRows()
    {
        var rows = new[] { new ProjectDto { Id = Guid.NewGuid(), Title = "Book" } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);

        var sut = new ProjectReadRepository(db.Object);
        var result = await sut.GetUserProjectsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProjectByIdAsync_ShouldReturnProjectDto()
    {
        var dto = new ProjectDto { Id = Guid.NewGuid(), Title = "Book" };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var sut = new ProjectReadRepository(db.Object);
        var result = await sut.GetProjectByIdAsync(dto.Id, Guid.NewGuid(), CancellationToken.None);

        result.Should().Be(dto);
    }

    [Fact]
    public async Task GetUserProjectByIdAsync_ShouldReturnEntity()
    {
        var entity = new Project { Id = Guid.NewGuid(), Title = "Book" };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<Project>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var sut = new ProjectReadRepository(db.Object);
        var result = await sut.GetUserProjectByIdAsync(entity.Id, Guid.NewGuid(), CancellationToken.None);

        result.Should().Be(entity);
    }
}
