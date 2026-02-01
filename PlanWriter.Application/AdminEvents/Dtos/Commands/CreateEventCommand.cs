using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.AdminEvents.Dtos.Commands;

public class CreateEventCommand(CreateEventRequest request) : IRequest<EventDto>
{
    public CreateEventRequest Event { get; } = request;
}