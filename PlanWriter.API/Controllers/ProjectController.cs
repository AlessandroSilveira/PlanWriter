using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;
using AddProjectProgressDto = PlanWriter.Application.DTO.AddProjectProgressDto;
using CreateProjectDto = PlanWriter.Application.DTO.CreateProjectDto;

namespace PlanWriter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController(IProjectService projectService) : ControllerBase
    {
        /// <summary>
        /// Create a new project
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            await projectService.CreateProjectAsync(dto, User);
            return Ok(new { Message = "Project created successfully." });
        }
        
        /// <summary>
        /// Adiciona uma entrada de progresso ao projeto.
        /// Add a new progress entry to project
        /// </summary>
        [HttpPost("progress")]
        public async Task<IActionResult> AddProgress([FromBody] AddProjectProgressDto dto)
        {
            await projectService.AddProgressAsync(dto, User);
            return Ok(new { message = "Progress added successfully." });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var projects = await projectService.GetUserProjectsAsync(User);
            return Ok(projects);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var project = await projectService.GetProjectByIdAsync(id, User);
            return Ok(project);   
        }
    
        [HttpPost("{id}/goal")]
        public async Task<IActionResult> SetGoal(Guid id, [FromBody] SetGoalDto dto)
        {
            var userId = projectService.GetUserId(User);
            await projectService.SetGoalAsync(id, userId, dto.WordCountGoal, dto.Deadline);
            return Ok(new { message = "Goal set successfully." });
        }
    
        [HttpPost("{id}/progress")]
        public async Task<IActionResult> GetProgressHistory(Guid id)
        {
            var history = await projectService.GetProgressHistoryAsync(id, User);
            return Ok(history);   
        }
    
        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetStats(Guid id)
        {
            var userId = projectService.GetUserId(User);
            var stats = await projectService.GetStatisticsAsync(id, userId);
            return Ok(stats);   
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = projectService.GetUserId(User);
            await projectService.DeleteProjectAsync(id, userId);
            return Ok(new { message = "Project deleted successfully." });   
        }
    }
}