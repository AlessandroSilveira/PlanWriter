using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Application.Auth.Commands;

public sealed class StartAdminMfaEnrollmentCommandHandler(
    IUserReadRepository userReadRepository,
    IAdminMfaRepository adminMfaRepository,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
    : IRequestHandler<StartAdminMfaEnrollmentCommand, AdminMfaEnrollmentDto>
{
    public async Task<AdminMfaEnrollmentDto> Handle(StartAdminMfaEnrollmentCommand request, CancellationToken ct)
    {
        var user = await userReadRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        if (!user.IsAdmin)
        {
            throw new BusinessRuleException("Somente administradores podem configurar MFA.");
        }

        if (user.AdminMfaEnabled)
        {
            throw new BusinessRuleException("MFA já está habilitado para este administrador.");
        }

        var secret = AdminMfaSecurity.GenerateSecretKey();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await adminMfaRepository.SetPendingSecretAsync(user.Id, secret, now, ct);

        var issuer = string.IsNullOrWhiteSpace(jwtOptions.Value.Issuer)
            ? "PlanWriter"
            : jwtOptions.Value.Issuer.Trim();

        return new AdminMfaEnrollmentDto
        {
            SecretKey = secret,
            OtpAuthUri = AdminMfaSecurity.BuildOtpAuthUri(issuer, user.Email, secret)
        };
    }
}
