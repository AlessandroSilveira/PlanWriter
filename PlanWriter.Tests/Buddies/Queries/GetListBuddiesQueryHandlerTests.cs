using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Buddies.Dtos.Queries;
using PlanWriter.Application.Buddies.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Buddies;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Buddies.Queries;

public class GetListBuddiesQueryHandlerTests
{
    private readonly Mock<IBuddiesRepository> _buddiesRepositoryMock = new();
    private readonly Mock<IUserFollowRepository> _userFollowRepositoryMock = new();
    private readonly Mock<ILogger<GetListBuddiesQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnBuddySummaries_WhenUserHasBuddies()
    {
        // Arrange
        var me = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var followeeIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var summaries = new List<BuddiesDto.BuddySummaryDto>
        {
            new BuddiesDto.BuddySummaryDto(
                followeeIds[0],
                "alice",
                "Alice Wonderland",
                "alice.png"
            ),
            new BuddiesDto.BuddySummaryDto(
                followeeIds[1],
                "bob",
                "Bob Builder",
                "bob.png"
            )
        };

        _userFollowRepositoryMock
            .Setup(r => r.GetFolloweeIdsAsync(me, cancellationToken))
            .ReturnsAsync(followeeIds);

        _buddiesRepositoryMock
            .Setup(r => r.GetBuddySummariesAsync(followeeIds, cancellationToken))
            .ReturnsAsync(summaries);

        var handler = CreateHandler();
        var query = new GetListBuddiesQuery(me);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(summaries);

        _userFollowRepositoryMock.Verify(
            r => r.GetFolloweeIdsAsync(me, cancellationToken),
            Times.Once
        );

        _buddiesRepositoryMock.Verify(
            r => r.GetBuddySummariesAsync(followeeIds, cancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoBuddies()
    {
        // Arrange
        var me = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var followeeIds = new List<Guid>();

        _userFollowRepositoryMock
            .Setup(r => r.GetFolloweeIdsAsync(me, cancellationToken))
            .ReturnsAsync(followeeIds);

        _buddiesRepositoryMock
            .Setup(r => r.GetBuddySummariesAsync(followeeIds, cancellationToken))
            .ReturnsAsync(new List<BuddiesDto.BuddySummaryDto>());

        var handler = CreateHandler();
        var query = new GetListBuddiesQuery(me);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _buddiesRepositoryMock.Verify(
            r => r.GetBuddySummariesAsync(followeeIds, cancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenUserFollowRepositoryThrows()
    {
        // Arrange
        var me = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _userFollowRepositoryMock
            .Setup(r => r.GetFolloweeIdsAsync(me, cancellationToken))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var handler = CreateHandler();
        var query = new GetListBuddiesQuery(me);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");

        _buddiesRepositoryMock.Verify(
            r => r.GetBuddySummariesAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    /* ===================== HELPERS ===================== */

    private GetListBuddiesQueryHandler CreateHandler()
    {
        return new GetListBuddiesQueryHandler(
            _buddiesRepositoryMock.Object,
            _userFollowRepositoryMock.Object,
            _loggerMock.Object
        );
    }
}