using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetActiveEventsQuery : IRequest<List<EventDto>>;

    
