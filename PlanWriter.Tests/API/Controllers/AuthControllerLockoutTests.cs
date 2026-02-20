using System.Net;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.API.Controllers;
using PlanWriter.API.Security;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using Xunit;

namespace PlanWriter.Tests.API.Controllers;

public class AuthControllerLockoutTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILoginLockoutService> _lockoutService = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 2, 20, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Login_ShouldReturnForbidden_WhenPreCheckIsLocked()
    {
        var request = new LoginUserDto
        {
            Email = "user@planwriter.com",
            Password = "Whatever123!"
        };

        _lockoutService
            .Setup(s => s.Check("user@planwriter.com", "10.0.0.1", It.IsAny<DateTime>()))
            .Returns(new LoginLockoutStatus(true, DateTime.UtcNow.AddMinutes(1), 5, 3));

        var controller = CreateController("10.0.0.1");

        var result = await controller.Login(request);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalidAndNotLocked()
    {
        var request = new LoginUserDto
        {
            Email = "user@planwriter.com",
            Password = "invalid-pass"
        };

        _lockoutService
            .Setup(s => s.Check("user@planwriter.com", "10.0.0.1", It.IsAny<DateTime>()))
            .Returns(new LoginLockoutStatus(false, null, 0, 0));

        _mediator
            .Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _lockoutService
            .Setup(s => s.RegisterFailure("user@planwriter.com", "10.0.0.1", It.IsAny<DateTime>()))
            .Returns(new LoginLockoutStatus(false, null, 1, 1));

        var controller = CreateController("10.0.0.1");

        var result = await controller.Login(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ShouldReturnForbidden_WhenFailureTriggersLockout()
    {
        var request = new LoginUserDto
        {
            Email = "user@planwriter.com",
            Password = "invalid-pass"
        };

        _lockoutService
            .Setup(s => s.Check("user@planwriter.com", "10.0.0.1", It.IsAny<DateTime>()))
            .Returns(new LoginLockoutStatus(false, null, 4, 2));

        _mediator
            .Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _lockoutService
            .Setup(s => s.RegisterFailure("user@planwriter.com", "10.0.0.1", It.IsAny<DateTime>()))
            .Returns(new LoginLockoutStatus(true, DateTime.UtcNow.AddMinutes(2), 5, 3));

        var controller = CreateController("10.0.0.1");

        var result = await controller.Login(request);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Login_ShouldReturnTokenAndResetCounters_WhenCredentialsAreValid()
    {
        var request = new LoginUserDto
        {
            Email = "user@planwriter.com",
            Password = "ValidPassword#2026"
        };

        _lockoutService
            .Setup(s => s.Check("user@planwriter.com", "10.0.0.1", It.IsAny<DateTime>()))
            .Returns(new LoginLockoutStatus(false, null, 0, 0));

        _mediator
            .Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("jwt-token");

        var controller = CreateController("10.0.0.1");

        var result = await controller.Login(request);

        result.Should().BeOfType<OkObjectResult>();
        _lockoutService.Verify(
            s => s.RegisterSuccess("user@planwriter.com", "10.0.0.1"),
            Times.Once);
    }

    private AuthController CreateController(string ipAddress)
    {
        var controller = new AuthController(
            _mediator.Object,
            _lockoutService.Object,
            _timeProvider,
            _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        return controller;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
