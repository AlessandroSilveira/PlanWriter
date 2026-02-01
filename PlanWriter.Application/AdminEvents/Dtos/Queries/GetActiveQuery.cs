using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.AdminEvents.Dtos.Queries;

public class GetActiveQuery : IRequest<List<EventDto>>;
