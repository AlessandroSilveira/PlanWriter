using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.DTOs;
using PlanWriter.Application.Services;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Interfaces;

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
            await projectService.CreateProjectAsync(dto);
            return Ok(new { Message = "Project created successfully." });
        }
        
        /// <summary>
        /// Adiciona uma entrada de progresso ao projeto.
        /// Add a new progress entry to project
        /// </summary>
        [HttpPost("progress")]
        public async Task<IActionResult> AddProgress([FromBody] AddProjectProgressDto dto)
        {
            await projectService.AddProgressAsync(dto);
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
            var project = await projectService.GetProjectByIdAsync(id);
            return Ok(project);   
        }
    
        [HttpPost("{id}/goal")]
        public async Task<IActionResult> SetGoal(Guid id, [FromBody] SetGoalDto dto)
        {
            await projectService.SetGoalAsync(id, dto, User);
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
            var stats = await projectService.GetStatisticsAsync(id, User);
            return Ok(stats);   
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await projectService.DeleteProjectAsync(id, User);
            return Ok(new { message = "Project deleted successfully." });   
        }
    }
}