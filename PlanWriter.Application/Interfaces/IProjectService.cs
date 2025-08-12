using PlanWriter.Application.DTOs;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Interfaces
{
    public interface IProjectService
    {
        Task CreateProjectAsync(CreateProjectDto dto);
        Task AddProgressAsync(AddProjectProgressDto dto);
    }
}