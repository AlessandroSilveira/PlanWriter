using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Queries;

public record GetWordWarByIdQuery(Guid WarId) : IRequest<WordWarDto>
{
    
}