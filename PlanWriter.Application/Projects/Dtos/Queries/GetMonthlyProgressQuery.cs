using System;
using MediatR;

namespace PlanWriter.Application.Projects.Dtos.Queries;

public record GetMonthlyProgressQuery(Guid UserId) : IRequest<int>;

    
