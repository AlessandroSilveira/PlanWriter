using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Application.Auth.Commands;

public sealed class ConfirmAdminMfaEnrollmentCommandHandler(
    IUserReadRepository userReadRepository,
    IAdminMfaRepository adminMfaRepository,
    TimeProvider timeProvider)
    : IRequestHandler<ConfirmAdminMfaEnrollmentCommand, AdminMfaBackupCodesDto>
{
    public async Task<AdminMfaBackupCodesDto> Handle(ConfirmAdminMfaEnrollmentCommand request, CancellationToken ct)
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

        if (string.IsNullOrWhiteSpace(user.AdminMfaPendingSecret))
        {
            throw new BusinessRuleException("Inicie o enrollment do MFA antes de confirmar.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var isCodeValid = AdminMfaSecurity.ValidateTotpCode(user.AdminMfaPendingSecret, request.Code, now);
        if (!isCodeValid)
        {
            throw new BusinessRuleException("Código MFA inválido.");
        }

        await adminMfaRepository.EnableAsync(user.Id, user.AdminMfaPendingSecret, ct);

        var backupCodes = AdminMfaSecurity.GenerateBackupCodes();
        var hashes = backupCodes.Select(AdminMfaSecurity.HashBackupCode).ToArray();
        await adminMfaRepository.ReplaceBackupCodesAsync(user.Id, hashes, ct);

        return new AdminMfaBackupCodesDto
        {
            BackupCodes = backupCodes
        };
    }
}
