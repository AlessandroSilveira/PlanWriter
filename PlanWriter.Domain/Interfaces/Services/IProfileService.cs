using System;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IProfileService
{
    Task<MyProfileDto> GetMineAsync(Guid userId);
    Task<MyProfileDto> UpdateMineAsync(Guid userId, UpdateMyProfileRequest req);
    Task<PublicProfileDto> GetPublicAsync(string slug);
}