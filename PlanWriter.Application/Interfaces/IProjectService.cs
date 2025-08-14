﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using AddProjectProgressDto = PlanWriter.Application.DTO.AddProjectProgressDto;
using CreateProjectDto = PlanWriter.Application.DTO.CreateProjectDto;

namespace PlanWriter.Application.Interfaces
{
    public interface IProjectService
    {
        string GetUserId(ClaimsPrincipal user);
        Task CreateProjectAsync(CreateProjectDto dto, ClaimsPrincipal user);
        Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(ClaimsPrincipal user);
        Task<ProjectDto> GetProjectByIdAsync(Guid id, ClaimsPrincipal user);
        Task AddProgressAsync(AddProjectProgressDto dto, ClaimsPrincipal user);
        Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user);
        Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline);
        Task<bool> DeleteProjectAsync(Guid projectId, string userId);
        Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId);
    }
}