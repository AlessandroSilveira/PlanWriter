using System;
using MediatR;
using PlanWriter.Domain.Requests;

namespace PlanWriter.Application.AdminEvents.Dtos.Commands;

public class UpdateAdminEventCommand(UpdateEventDto request, Guid id) : IRequest<Unit>
{
    public UpdateEventDto Request { get; } = request;
    public Guid Id { get; } = id;
}