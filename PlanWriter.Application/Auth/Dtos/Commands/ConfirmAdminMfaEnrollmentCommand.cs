using System;
using MediatR;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public sealed record ConfirmAdminMfaEnrollmentCommand(Guid UserId, string Code) : IRequest<AdminMfaBackupCodesDto>;
