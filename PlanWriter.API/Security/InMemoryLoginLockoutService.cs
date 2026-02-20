using System;
using System.Collections.Concurrent;
using System.Linq;

namespace PlanWriter.API.Security;

public sealed class InMemoryLoginLockoutService(LoginLockoutOptions? options = null) : ILoginLockoutService
{
    private readonly LoginLockoutOptions _options = options ?? new LoginLockoutOptions();
    private readonly ConcurrentDictionary<string, CounterState> _states = new(StringComparer.OrdinalIgnoreCase);

    public LoginLockoutStatus Check(string email, string? ipAddress, DateTime utcNow)
    {
        var userKey = BuildUserKey(email);
        var ipKey = BuildIpKey(ipAddress);

        var user = EvaluateState(userKey, _options.UserSteps, utcNow);
        var ip = EvaluateState(ipKey, _options.IpSteps, utcNow);

        return Merge(user, ip);
    }

    public LoginLockoutStatus RegisterFailure(string email, string? ipAddress, DateTime utcNow)
    {
        var userKey = BuildUserKey(email);
        var ipKey = BuildIpKey(ipAddress);

        var user = IncrementFailure(userKey, _options.UserSteps, utcNow);
        var ip = IncrementFailure(ipKey, _options.IpSteps, utcNow);

        return Merge(user, ip);
    }

    public void RegisterSuccess(string email, string? ipAddress)
    {
        var userKey = BuildUserKey(email);
        _states.TryRemove(userKey, out _);

        var ipKey = BuildIpKey(ipAddress);
        if (ipKey is not null)
        {
            _states.TryRemove(ipKey, out _);
        }
    }

    private CounterView EvaluateState(string? key, LockoutStep[] steps, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return CounterView.Empty;
        }

        if (!_states.TryGetValue(key, out var state))
        {
            return CounterView.Empty;
        }

        lock (state.Sync)
        {
            if (state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value <= utcNow)
            {
                state.LockedUntilUtc = null;
            }

            if (state.LastFailureUtc.HasValue && utcNow - state.LastFailureUtc.Value > _options.FailureWindow)
            {
                _states.TryRemove(key, out _);
                return CounterView.Empty;
            }

            var isLocked = state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value > utcNow;
            return new CounterView(
                isLocked,
                state.LockedUntilUtc,
                state.FailureCount
            );
        }
    }

    private CounterView IncrementFailure(string? key, LockoutStep[] steps, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return CounterView.Empty;
        }

        var state = _states.GetOrAdd(key, _ => new CounterState());

        lock (state.Sync)
        {
            if (state.LastFailureUtc.HasValue && utcNow - state.LastFailureUtc.Value > _options.FailureWindow)
            {
                state.FailureCount = 0;
                state.LockedUntilUtc = null;
            }

            state.FailureCount++;
            state.LastFailureUtc = utcNow;

            var step = steps
                .Where(s => state.FailureCount >= s.Failures)
                .OrderBy(s => s.Failures)
                .LastOrDefault();

            if (step != default)
            {
                var until = utcNow.Add(step.Duration);
                if (!state.LockedUntilUtc.HasValue || until > state.LockedUntilUtc.Value)
                {
                    state.LockedUntilUtc = until;
                }
            }

            var isLocked = state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value > utcNow;
            return new CounterView(
                isLocked,
                state.LockedUntilUtc,
                state.FailureCount
            );
        }
    }

    private static LoginLockoutStatus Merge(CounterView user, CounterView ip)
    {
        var isLocked = user.IsLocked || ip.IsLocked;
        var maxLockUntil = MaxDateTime(user.LockedUntilUtc, ip.LockedUntilUtc);

        return new LoginLockoutStatus(
            IsLocked: isLocked,
            LockedUntilUtc: maxLockUntil,
            UserFailureCount: user.FailureCount,
            IpFailureCount: ip.FailureCount
        );
    }

    private static string BuildUserKey(string email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        return $"usr:{normalized}";
    }

    private static string? BuildIpKey(string? ipAddress)
    {
        var normalized = (ipAddress ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : $"ip:{normalized}";
    }

    private static DateTime? MaxDateTime(DateTime? left, DateTime? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return left.Value >= right.Value ? left : right;
    }

    private sealed class CounterState
    {
        public object Sync { get; } = new();
        public int FailureCount { get; set; }
        public DateTime? LastFailureUtc { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
    }

    private readonly record struct CounterView(bool IsLocked, DateTime? LockedUntilUtc, int FailureCount)
    {
        public static CounterView Empty => new(false, null, 0);
    }
}
