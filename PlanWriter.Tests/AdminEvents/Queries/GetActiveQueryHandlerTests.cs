// using FluentAssertions;
// using Microsoft.Extensions.Logging;
// using Moq;
// using PlanWriter.Application.AdminEvents.Dtos.Queries;
// using PlanWriter.Application.AdminEvents.Queries;
// using PlanWriter.Domain.Dtos;
// using PlanWriter.Domain.Interfaces.Repositories;
// using PlanWriter.Infrastructure.ReadModels.Events.Admin;
// using Xunit;
//
// namespace PlanWriter.Tests.AdminEvents.Queries;
//
// public class GetActiveQueryHandlerTests
// {
//     [Fact]
//     public async Task Handle_ShouldReturnActiveEvents()
//     {
//         // Arrange
//         var now = DateTime.UtcNow;
//
//         var events = new List<EventDto>
//         {
//             new EventDto(
//                 Id: Guid.NewGuid(),
//                 Name: "Evento Ativo 1",
//                 Slug: "evento-ativo-1",
//                 Type: "nano",
//                 StartsAtUtc: now.AddDays(-5),
//                 EndsAtUtc: now.AddDays(25),
//                 DefaultTargetWords: 50000,
//                 IsActive: true
//             ),
//             new EventDto(
//                 Id: Guid.NewGuid(),
//                 Name: "Evento Ativo 2",
//                 Slug: "evento-ativo-2",
//                 Type: "custom",
//                 StartsAtUtc: now.AddDays(-1),
//                 EndsAtUtc: now.AddDays(10),
//                 DefaultTargetWords: null,
//                 IsActive: true
//             )
//         };
//
//         var repositoryMock = new Mock<IEventRepository>();
//         repositoryMock
//             .Setup(r => r.GetActiveEvents())
//             .ReturnsAsync(events);
//
//         var logMock = new Mock<ILogger<EventReadRepository>>();
//
//         var handler = new GetActiveQueryHandler(repositoryMock.Object, logMock.Object);
//         var query = new GetActiveQuery();
//
//         // Act
//         var result = await handler.Handle(query, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         result.Should().HaveCount(2);
//         result.Should().BeEquivalentTo(events);
//
//         repositoryMock.Verify(
//             r => r.GetActiveEvents(),
//             Times.Once
//         );
//     }
//     
//     [Fact]
//     public async Task Handle_ShouldReturnEmptyList_WhenNoActiveEventsExist()
//     {
//         // Arrange
//         var repositoryMock = new Mock<IEventRepository>();
//         var logMock = new Mock<ILogger<GetActiveQueryHandler>>();
//
//         repositoryMock
//             .Setup(r => r.GetActiveEvents())
//             .ReturnsAsync(new List<EventDto>()); // 👈 lista vazia
//
//         var handler = new GetActiveQueryHandler(repositoryMock.Object, logMock.Object);
//         var query = new GetActiveQuery();
//
//         // Act
//         var result = await handler.Handle(query, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();        // nunca null
//         result.Should().BeEmpty();          // lista vazia esperada
//
//         repositoryMock.Verify(
//             r => r.GetActiveEvents(),
//             Times.Once
//         );
//     }
// }