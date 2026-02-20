using System;

namespace PlanWriter.API.Security;

public sealed class LoginLockoutOptions
{
    public TimeSpan FailureWindow { get; init; } = TimeSpan.FromMinutes(30);

    public LockoutStep[] UserSteps { get; init; } =
    [
        new(5, TimeSpan.FromMinutes(1)),
        new(8, TimeSpan.FromMinutes(5)),
        new(10, TimeSpan.FromMinutes(15))
    ];

    public LockoutStep[] IpSteps { get; init; } =
    [
        new(10, TimeSpan.FromMinutes(1)),
        new(20, TimeSpan.FromMinutes(5)),
        new(30, TimeSpan.FromMinutes(15))
    ];
}

public readonly record struct LockoutStep(int Failures, TimeSpan Duration);
