using System;
using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.AdminEvents;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.AdminEvents.Dtos.Commands;

public record CreateAdminEventCommand(CreateAdminEventRequest Event) : IRequest<EventDto?>;