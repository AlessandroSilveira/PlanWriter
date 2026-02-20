using FluentAssertions;
using PlanWriter.API.Security;
using Xunit;

namespace PlanWriter.Tests.Security;

public class InMemoryLoginLockoutServiceTests
{
    [Fact]
    public void RegisterFailure_ShouldLockUser_WhenThresholdIsReached()
    {
        var service = new InMemoryLoginLockoutService(new LoginLockoutOptions
        {
            UserSteps =
            [
                new LockoutStep(3, TimeSpan.FromMinutes(1))
            ],
            IpSteps =
            [
                new LockoutStep(100, TimeSpan.FromMinutes(1))
            ]
        });

        var now = Utc(2026, 2, 20, 18, 0, 0);

        service.RegisterFailure("user@planwriter.com", "10.0.0.10", now);
        service.RegisterFailure("user@planwriter.com", "10.0.0.10", now.AddSeconds(1));
        var status = service.RegisterFailure("user@planwriter.com", "10.0.0.10", now.AddSeconds(2));

        status.IsLocked.Should().BeTrue();
        status.UserFailureCount.Should().Be(3);
        status.LockedUntilUtc.Should().Be(now.AddSeconds(2).AddMinutes(1));
    }

    [Fact]
    public void Check_ShouldUnlock_WhenLockDurationExpires()
    {
        var service = new InMemoryLoginLockoutService(new LoginLockoutOptions
        {
            UserSteps =
            [
                new LockoutStep(2, TimeSpan.FromSeconds(30))
            ],
            IpSteps =
            [
                new LockoutStep(100, TimeSpan.FromMinutes(1))
            ]
        });

        var now = Utc(2026, 2, 20, 18, 10, 0);

        service.RegisterFailure("user@planwriter.com", "10.0.0.10", now);
        service.RegisterFailure("user@planwriter.com", "10.0.0.10", now.AddSeconds(1));

        var locked = service.Check("user@planwriter.com", "10.0.0.10", now.AddSeconds(10));
        locked.IsLocked.Should().BeTrue();

        var unlocked = service.Check("user@planwriter.com", "10.0.0.10", now.AddMinutes(1));
        unlocked.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void RegisterFailure_ShouldUseIpFallback_WhenUsersChange()
    {
        var service = new InMemoryLoginLockoutService(new LoginLockoutOptions
        {
            UserSteps =
            [
                new LockoutStep(100, TimeSpan.FromMinutes(1))
            ],
            IpSteps =
            [
                new LockoutStep(4, TimeSpan.FromMinutes(2))
            ]
        });

        var ip = "10.0.0.9";
        var now = Utc(2026, 2, 20, 18, 20, 0);

        service.RegisterFailure("a@planwriter.com", ip, now);
        service.RegisterFailure("b@planwriter.com", ip, now.AddSeconds(1));
        service.RegisterFailure("c@planwriter.com", ip, now.AddSeconds(2));
        var status = service.RegisterFailure("d@planwriter.com", ip, now.AddSeconds(3));

        status.IsLocked.Should().BeTrue();
        status.IpFailureCount.Should().Be(4);
        status.UserFailureCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterFailure_ShouldBeThreadSafe_UnderConcurrency()
    {
        var service = new InMemoryLoginLockoutService(new LoginLockoutOptions
        {
            UserSteps =
            [
                new LockoutStep(5, TimeSpan.FromMinutes(1))
            ],
            IpSteps =
            [
                new LockoutStep(100, TimeSpan.FromMinutes(1))
            ]
        });

        var now = Utc(2026, 2, 20, 18, 30, 0);
        var tasks = Enumerable
            .Range(0, 50)
            .Select(_ => Task.Run(() =>
                service.RegisterFailure("parallel@planwriter.com", "10.0.0.88", now)))
            .ToArray();

        await Task.WhenAll(tasks);

        var status = service.Check("parallel@planwriter.com", "10.0.0.88", now);
        status.IsLocked.Should().BeTrue();
        status.UserFailureCount.Should().Be(50);
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }
}
