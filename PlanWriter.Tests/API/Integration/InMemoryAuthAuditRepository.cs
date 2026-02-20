using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryAuthAuditRepository :
    IAuthAuditRepository,
    IAuthAuditReadRepository
{
    private readonly object _lock = new();
    private readonly List<AuthAuditLogDto> _items = [];

    public void Reset()
    {
        lock (_lock)
        {
            _items.Clear();
        }
    }

    public Task CreateAsync(
        Guid? userId,
        string eventType,
        string result,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        string? correlationId,
        string? details,
        CancellationToken ct)
    {
        lock (_lock)
        {
            _items.Add(new AuthAuditLogDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType,
                Result = result,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = traceId,
                CorrelationId = correlationId,
                Details = details,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuthAuditLogDto>> GetAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? userId,
        string? eventType,
        string? result,
        int limit,
        CancellationToken ct)
    {
        lock (_lock)
        {
            var query = _items.AsEnumerable();

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= toUtc.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(x => string.Equals(x.EventType, eventType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(result))
            {
                query = query.Where(x => string.Equals(x.Result, result, StringComparison.OrdinalIgnoreCase));
            }

            var data = query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(Math.Max(1, limit))
                .ToList();

            return Task.FromResult<IReadOnlyList<AuthAuditLogDto>>(data);
        }
    }
}
