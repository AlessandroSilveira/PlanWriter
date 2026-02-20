using System;
using MediatR;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public sealed class LogoutAllSessionsCommand(Guid userId, string? ipAddress, string? device) : IRequest<int>
{
    public Guid UserId { get; } = userId;
    public string? IpAddress { get; } = ipAddress;
    public string? Device { get; } = device;
}
