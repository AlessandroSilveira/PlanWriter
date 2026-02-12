using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Certificates;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Certificates;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Certificates;

public class CertificateReadRepositoryTests
{
    [Fact]
    public async Task GetWinnerRowAsync_ShouldReturnRow()
    {
        var expected = new CertificateWinnerRow
        {
            EventName = "NaNoWriMo",
            ProjectTitle = "Book",
            Won = true
        };

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<CertificateWinnerRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new CertificateReadRepository(db.Object);

        var result = await sut.GetWinnerRowAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().Be(expected);
    }
}
