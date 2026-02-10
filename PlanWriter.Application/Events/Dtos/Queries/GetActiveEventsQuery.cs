using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetActiveEventsQuery : IRequest<IReadOnlyList<EventDto>>;

    
