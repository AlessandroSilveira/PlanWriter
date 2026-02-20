using MediatR;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public sealed class LogoutSessionCommand(RefreshTokenDto request) : IRequest<bool>
{
    public RefreshTokenDto Request { get; } = request;
}
