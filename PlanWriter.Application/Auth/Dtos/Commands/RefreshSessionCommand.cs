using MediatR;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public sealed class RefreshSessionCommand(
    RefreshTokenDto request,
    string? ipAddress = null,
    string? device = null) : IRequest<AuthTokensDto?>
{
    public RefreshTokenDto Request { get; } = request;
    public string? IpAddress { get; } = ipAddress;
    public string? Device { get; } = device;
}
