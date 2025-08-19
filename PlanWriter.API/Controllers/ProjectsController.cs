using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;


namespace PlanWriter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController(IProjectService projectService) : ControllerBase
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
        [HttpPost("{id:guid}/progress")]
        public async Task<IActionResult> AddProgress(Guid id, [FromBody] AddProjectProgressDto dto)
        {
            if (dto == null)
                return BadRequest("Body inválido.");

            // força o ProjectId pelo route param
            dto.ProjectId = id;

            if (dto.WordsWritten <= 0)
                return BadRequest("TotalWordsWritten deve ser maior que zero.");

            if (dto.Date == default)
                dto.Date = DateTime.UtcNow;

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


        /// <summary>
        /// Retorna estatísticas de progresso para o projeto
        /// </summary>
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<ProjectStatsDto>> GetStats(Guid id)
        {   

            var stats = await projectService.GetStatsAsync(id, User);
            if (stats == null)
                return NotFound();

            return Ok(stats);
        }
        
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = projectService.GetUserId(User);
            await projectService.DeleteProjectAsync(id, userId);
            return Ok(new { message = "Project deleted successfully." });   
        }
        
        [HttpDelete("/progress/{progressId}")]
        public async Task<IActionResult> DeleteProgress(Guid progressId)
        {
            var userId = projectService.GetUserId(User);

            var result = await projectService.DeleteProgressAsync(progressId, userId);
            if (!result)
                return NotFound(new { message = "Progress not found or not authorized." });

            return NoContent();
        }
        
        [HttpGet("{id:guid}/progress")]
        public async Task<IActionResult> GetProgresses(Guid id)
        {
            var userId = projectService.GetUserId(User);

            var result = await projectService.GetProgressHistoryAsync(id, User);
            if (!result.Any())
                return NotFound(new { message = "Progress not found or not authorized." });

            return Ok(result);
        }
        
    }
}