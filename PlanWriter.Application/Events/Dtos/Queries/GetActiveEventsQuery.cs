using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetActiveEventsQuery : IRequest<List<EventDto>>;

    
