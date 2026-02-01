using System.Data;
using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using Xunit;

namespace PlanWriter.Tests.Repositorios.Projects;

public class ProjectRepositoryDapperTests
{
    private readonly Mock<IDbConnectionFactory> _connFactory = new();
    private readonly Mock<IDbConnection> _conn = new();
    private readonly Mock<IDbExecutor> _db = new();

    [Fact]
    public async Task CreateAsync_ShouldExecuteInsert_WithExpectedParams()
    {
        // Arrange
        _conn.SetupGet(c => c.State).Returns(ConnectionState.Open);
        _connFactory.Setup(f => f.CreateConnection()).Returns(_conn.Object);

        object? capturedParams = null;

        _db.Setup(x => x.ExecuteAsync(
                _conn.Object,
                It.Is<string>(sql => sql.Contains("INSERT INTO Projects")),
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<IDbConnection, string, object?, IDbTransaction?, CancellationToken>((_, _, p, _, _) => capturedParams = p)
            .ReturnsAsync(1);

        var repo = new ProjectRepositoryDapper(_connFactory.Object, _db.Object);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "X",
            Description = "Y",
            Genre = "Z",
            WordCountGoal = 1000,
            GoalAmount = 1000,
            GoalUnit = GoalUnit.Words,
            StartDate = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow,
            CurrentWordCount = 0,
            IsPublic = false
        };

        // Act
        await repo.CreateAsync(project, CancellationToken.None);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.ToString().Should().NotBeNull(); // só garante que capturou
        _db.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenAffectedRowsNotOne()
    {
        // Arrange
        _conn.SetupGet(c => c.State).Returns(ConnectionState.Open);
        _connFactory.Setup(f => f.CreateConnection()).Returns(_conn.Object);

        _db.Setup(x => x.ExecuteAsync(
                _conn.Object,
                It.IsAny<string>(),
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var repo = new ProjectRepositoryDapper(_connFactory.Object, _db.Object);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "X",
            GoalUnit = GoalUnit.Words,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        Func<Task> act = async () => await repo.CreateAsync(project, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insert Projects expected 1 row, affected=0.");
    }
}
