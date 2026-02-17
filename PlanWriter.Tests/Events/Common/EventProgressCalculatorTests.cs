using FluentAssertions;
using PlanWriter.Application.Common.Events;
using Xunit;

namespace PlanWriter.Tests.Events.Common;

public class EventProgressCalculatorTests
{
    private readonly EventProgressCalculator _sut = new();

    [Fact]
    public void Calculate_ShouldUseProvidedTarget_WhenTargetIsPositive()
    {
        var result = _sut.Calculate(1000, 50000, 400);

        result.TargetWords.Should().Be(1000);
        result.TotalWords.Should().Be(400);
        result.Percent.Should().Be(40);
        result.RemainingWords.Should().Be(600);
        result.Won.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ShouldFallbackToDefaultTarget_WhenTargetIsNullOrZero()
    {
        var resultFromNull = _sut.Calculate(null, null, 500);
        var resultFromZero = _sut.Calculate(0, null, 500);

        resultFromNull.TargetWords.Should().Be(50000);
        resultFromZero.TargetWords.Should().Be(50000);
        resultFromNull.Percent.Should().Be(1);
        resultFromZero.Percent.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldMarkWon_WhenTotalReachesTarget()
    {
        var result = _sut.Calculate(1000, null, 1200);

        result.Won.Should().BeTrue();
        result.RemainingWords.Should().Be(0);
        result.Percent.Should().Be(120);
    }

    [Fact]
    public void ResolveWindowEndExclusive_ShouldNormalizeToNextDay()
    {
        var endAt = new DateTime(2026, 2, 20, 11, 45, 0, DateTimeKind.Utc);

        var endExclusive = _sut.ResolveWindowEndExclusive(endAt);

        endExclusive.Should().Be(new DateTime(2026, 2, 21, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Calculate_ShouldFallbackToEventDefault_WhenProjectTargetIsNullOrZero()
    {
        var resultFromNull = _sut.Calculate(null, 42000, 1000);
        var resultFromZero = _sut.Calculate(0, 42000, 1000);

        resultFromNull.TargetWords.Should().Be(42000);
        resultFromZero.TargetWords.Should().Be(42000);
        resultFromNull.Percent.Should().Be(2);
        resultFromZero.Percent.Should().Be(2);
    }
}
