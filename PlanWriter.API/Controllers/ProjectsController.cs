using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;
using IProjectService = PlanWriter.Application.Interfaces.IProjectService;

namespace PlanWriter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController(
        IProjectService projectService,
        IUserService userService,
        IBadgeServices badgeServices) : ControllerBase
    {
        /// <summary>
        /// Create a new project (JSON). Returns the created project with Id.
        /// </summary>
        [HttpPost]
        
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var project = await projectService.CreateProjectAsync(dto, User);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }

        /// <summary>
        /// Get all projects of current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var projects = await projectService.GetUserProjectsAsync(User);
            return Ok(projects);
        }

        /// <summary>
        /// Get project by Id
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var project = await projectService.GetProjectByIdAsync(id, User);
            if (project is null) return NotFound();
            return Ok(project);
        }

        /// <summary>
        /// Set goal (word count + deadline)
        /// </summary>
        [HttpPost("{id:guid}/goal")]
        public async Task<IActionResult> SetGoal(Guid id, [FromBody] SetGoalDto dto)
        {
            var userId = userService.GetUserId(User);
            await projectService.SetGoalAsync(id, userId, dto.WordCountGoal, dto.Deadline);
            return Ok(new { message = "Goal set successfully." });
        }

        /// <summary>
        /// Add a new progress entry to project
        /// </summary>
        [HttpPost("{id:guid}/progress")]
        public async Task<IActionResult> AddProgress(Guid id, [FromBody] AddProjectProgressDto dto)
        {
            if (dto == null) return BadRequest("Body inválido.");

            dto.ProjectId = id; // força pelo route param
            if (dto.WordsWritten <= 0) return BadRequest("TotalWordsWritten deve ser maior que zero.");
            if (dto.Date == default) dto.Date = DateTime.UtcNow;

            await projectService.AddProgressAsync(dto, User);
            await badgeServices.CheckAndAssignBadgesAsync(id, User);

            return Ok(new { message = "Progress added successfully." });
        }

        /// <summary>
        /// Get project progress history
        /// </summary>
        [HttpGet("{id:guid}/progress")]
        public async Task<IActionResult> GetProgresses(Guid id)
        {
            var result = await projectService.GetProgressHistoryAsync(id, User);
            if (!result.Any())
                return NotFound(new { message = "Progress not found or not authorized." });
            return Ok(result);
        }

        /// <summary>
        /// Delete a progress entry
        /// </summary>
        [HttpDelete("progress/{progressId:guid}")]
        public async Task<IActionResult> DeleteProgress(Guid progressId)
        {
            var userId = userService.GetUserId(User);
            var ok = await projectService.DeleteProgressAsync(progressId, userId);
            if (!ok) return NotFound(new { message = "Progress not found or not authorized." });
            return NoContent();
        }

        /// <summary>
        /// Get project badges (calculates/assigns if needed)
        /// </summary>
        [HttpGet("{projectId:guid}/badges")]
        public async Task<IActionResult> GetBadges(Guid projectId)
        {
            var badges = await badgeServices.CheckAndAssignBadgesAsync(projectId, User);
            return Ok(badges);
        }

        /// <summary>
        /// Project stats
        /// </summary>
        [HttpGet("{id:guid}/stats")]
        public async Task<ActionResult<ProjectStatsDto>> GetStats(Guid id)
        {
            var stats = await projectService.GetStatsAsync(id, User);
            if (stats == null) return NotFound();
            return Ok(stats);
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = userService.GetUserId(User);
            await projectService.DeleteProjectAsync(id, userId);
            return Ok(new { message = "Project deleted successfully." });
        }

        
    }
}
