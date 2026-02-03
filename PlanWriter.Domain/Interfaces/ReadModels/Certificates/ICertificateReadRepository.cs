using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Certificates;

namespace PlanWriter.Domain.Interfaces.ReadModels.Certificates;

public interface ICertificateReadRepository
{
    Task<CertificateWinnerRow?> GetWinnerRowAsync(Guid projectId, Guid eventId, Guid userId, CancellationToken ct);
}