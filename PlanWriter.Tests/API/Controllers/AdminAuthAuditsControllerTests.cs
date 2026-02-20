using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using PlanWriter.API.Controllers;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using Xunit;

namespace PlanWriter.Tests.API.Controllers;

public class AdminAuthAuditsControllerTests
{
    [Fact]
    public async Task Get_ShouldClampLimit_AndApplyDefaultFromDate()
    {
        DateTime? capturedFrom = null;
        int capturedLimit = 0;

        var readRepository = new Mock<IAuthAuditReadRepository>();
        readRepository
            .Setup(r => r.GetAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<DateTime?, DateTime?, Guid?, string?, string?, int, CancellationToken>((from, _, _, _, _, limit, _) =>
            {
                capturedFrom = from;
                capturedLimit = limit;
            })
            .ReturnsAsync(new List<AuthAuditLogDto>());

        var options = Options.Create(new AuthAuditOptions
        {
            RetentionDays = 30,
            MaxReadLimit = 200
        });

        var controller = new AdminAuthAuditsController(readRepository.Object, options);
        var result = await controller.Get(
            fromUtc: null,
            toUtc: null,
            userId: null,
            eventType: null,
            result: null,
            limit: 999,
            ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        capturedFrom.Should().NotBeNull();
        capturedLimit.Should().Be(200);
    }
}
