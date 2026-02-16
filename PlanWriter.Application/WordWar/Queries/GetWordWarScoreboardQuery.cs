using System;
using MediatR;
using PlanWriter.Domain.Dtos.WordWars;

namespace PlanWriter.Application.WordWar.Queries;

public record GetWordWarScoreboardQuery(Guid WarId) : IRequest<WordWarScoreboardDto>
{
    
}