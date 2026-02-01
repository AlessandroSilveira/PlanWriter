using MediatR;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public class LoginUserCommand(LoginUserDto request) : IRequest<string?>
{
    public LoginUserDto Request { get; } = request;
}