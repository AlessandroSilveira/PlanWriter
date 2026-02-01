using System;
using MediatR;

namespace PlanWriter.Application.Certificates.Dtos.Queries;

public class GetCertificateQuery(Guid eventId, Guid projectId, string userName) : IRequest<byte[]>
{
    public Guid EventId { get; } = eventId;
    public Guid ProjectId { get; } = projectId;
    public string UserName { get; } = userName;
}