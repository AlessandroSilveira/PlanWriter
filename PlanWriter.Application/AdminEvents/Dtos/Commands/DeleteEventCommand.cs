using System;
using MediatR;

namespace PlanWriter.Application.AdminEvents.Dtos.Commands;

public class DeleteEventCommand(Guid id): IRequest<Unit>
{
    public Guid Id { get; } = id;
}