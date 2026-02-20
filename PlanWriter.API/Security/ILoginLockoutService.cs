using System;

namespace PlanWriter.API.Security;

public interface ILoginLockoutService
{
    LoginLockoutStatus Check(string email, string? ipAddress, DateTime utcNow);
    LoginLockoutStatus RegisterFailure(string email, string? ipAddress, DateTime utcNow);
    void RegisterSuccess(string email, string? ipAddress);
}

public sealed record LoginLockoutStatus(
    bool IsLocked,
    DateTime? LockedUntilUtc,
    int UserFailureCount,
    int IpFailureCount
);
