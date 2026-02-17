using FluentAssertions;
using PlanWriter.Application.Common.WinnerEligibility;
using Xunit;

namespace PlanWriter.Tests.Common.WinnerEligibility;

public class WinnerEligibilityServiceTests
{
    private readonly WinnerEligibilityService _sut = new();

    [Fact]
    public void EvaluateForGoodies_ShouldReturnEligible_WhenValidatedWinner()
    {
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var end = now.AddDays(1);

        var result = _sut.EvaluateForGoodies(now, end, 50000, 50000, true, true);

        result.IsEligible.Should().BeTrue();
        result.Status.Should().Be("eligible");
    }

    [Fact]
    public void EvaluateForGoodies_ShouldReturnPendingValidation_WhenTargetReachedButNotValidated()
    {
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var end = now.AddDays(1);

        var result = _sut.EvaluateForGoodies(now, end, 50000, 50000, false, false);

        result.IsEligible.Should().BeFalse();
        result.CanValidate.Should().BeTrue();
        result.Status.Should().Be("pending_validation");
    }

    [Fact]
    public void EvaluateForGoodies_ShouldReturnInProgress_WhenEventStillOpenAndTargetNotReached()
    {
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var end = now.AddDays(1);

        var result = _sut.EvaluateForGoodies(now, end, 50000, 12000, false, false);

        result.Status.Should().Be("in_progress");
        result.CanValidate.Should().BeFalse();
    }

    [Fact]
    public void EvaluateForGoodies_ShouldReturnNotEligible_WhenEventEndedAndTargetNotReached()
    {
        var now = new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc);
        var end = now.AddDays(-1);

        var result = _sut.EvaluateForGoodies(now, end, 50000, 12000, false, false);

        result.Status.Should().Be("not_eligible");
        result.IsEligible.Should().BeFalse();
    }

    [Fact]
    public void EvaluateForCertificate_ShouldReturnNotEligible_WhenNotWinnerOrNotValidated()
    {
        var result = _sut.EvaluateForCertificate(false, true);

        result.IsEligible.Should().BeFalse();
        result.Status.Should().Be("not_eligible");
    }
}
