using System;
using MediatR;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Events;

public record ProjectProgressAdded(Guid ProjectId, Guid UserId, int NewTotal, GoalUnit GoalUnit) : INotification;