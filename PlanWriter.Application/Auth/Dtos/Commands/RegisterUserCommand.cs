using MediatR;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public class RegisterUserCommand(RegisterUserDto request) : IRequest<bool>
{
    public RegisterUserDto Request { get; } = request;
}