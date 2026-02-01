using System;
using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Requests;

namespace PlanWriter.Application.Profile.Dtos.Commands;

public record UpdateProfileCommand(Guid UserId, UpdateMyProfileRequest Request) : IRequest<MyProfileDto>;

    
