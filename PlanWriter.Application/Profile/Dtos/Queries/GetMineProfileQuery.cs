using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Profile.Dtos.Queries;

public record GetMineProfileQuery(Guid UserId) : IRequest<MyProfileDto>;

    
