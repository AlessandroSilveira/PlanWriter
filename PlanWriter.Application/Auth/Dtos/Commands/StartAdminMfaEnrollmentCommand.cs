using System;
using MediatR;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Application.Auth.Dtos.Commands;

public sealed record StartAdminMfaEnrollmentCommand(Guid UserId) : IRequest<AdminMfaEnrollmentDto>;
