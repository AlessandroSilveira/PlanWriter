using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.AdminEvents.Dtos.Queries;

public class GetActiveQuery : IRequest<IReadOnlyList<EventDto>>;
