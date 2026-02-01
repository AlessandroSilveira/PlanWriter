using System;
using MediatR;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public class ChangePasswordCommand(Guid userId, ChangePasswordDto request) : IRequest<string>
{
    public Guid UserId { get; } = userId;
    public ChangePasswordDto Request { get; } = request;
}