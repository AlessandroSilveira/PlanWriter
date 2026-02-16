using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Queries;

public record GetActiveWordWarByEventIdQuery(Guid WarId, Guid EventId) : IRequest<WordWarDto>;

    
