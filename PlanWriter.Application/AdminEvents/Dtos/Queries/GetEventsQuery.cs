using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.AdminEvents.Dtos.Queries;

public class GetEventsQuery : IRequest<List<EventDto>>;

    
