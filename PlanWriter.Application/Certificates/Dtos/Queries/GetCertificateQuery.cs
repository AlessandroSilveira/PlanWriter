using System;
using MediatR;

namespace PlanWriter.Application.Certificates.Dtos.Queries;

public record GetCertificateQuery(Guid EventId, Guid ProjectId, string UserName, Guid UserId) : IRequest<byte[]>;

   
