using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Enums; // GoalUnit
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
        IBadgeServices badgeServices,
        IMilestonesService milestonesService) : ControllerBase
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
        /// Set flexible goal (amount + unit + optional deadline)
        /// </summary>
        [HttpPost("{id:guid}/goal")]
        public async Task<IActionResult> SetGoal(Guid id, [FromBody] SetFlexibleGoalDto dto)
        {
            if (dto is null) return BadRequest("Body inválido.");
            if (dto.GoalAmount < 0) return BadRequest("GoalAmount deve ser >= 0.");

            var userId = userService.GetUserId(User);

            // Novo método do service (ver seção 3)
            await projectService.SetFlexibleGoalAsync(id, Guid.Parse(userId), dto.GoalAmount, dto.GoalUnit, dto.Deadline);

            return Ok(new { message = "Goal set successfully." });
        }

        /// <summary>
        /// Add a new progress entry (supports Words/Minutes/Pages)
        /// </summary>
        [HttpPost("{id:guid}/progress")]
        public async Task<IActionResult> AddProgress(Guid id, [FromBody] AddProjectProgressDto dto, CancellationToken ct = default)
        {
            if (dto == null) 
                return BadRequest("Body inválido.");

            dto.ProjectId = id;

            // Se não vier data, usa hoje (UTC)
            if (dto.Date == default) 
                dto.Date = DateTime.UtcNow;

            // Validação mínima
            var w = dto.WordsWritten.GetValueOrDefault();
            var m = dto.Minutes.GetValueOrDefault();
            var p = dto.Pages.GetValueOrDefault();

            if (w <= 0 && m <= 0 && p <= 0)
                return BadRequest("Informe WordsWritten, Minutes ou Pages com valor > 0.");

            // 🔥 CHAMA APENAS UMA VEZ!
            await projectService.AddProgressAsync(dto, User);

            // Recalcula stats
            var stats = await projectService.GetStatsAsync(id, User);
            var totalAccum = stats?.TotalWords ?? 0;

            // milestones automáticos
            await milestonesService.EvaluateAutoMilestonesAsync(id, totalAccum, ct);

            // badges
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

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(Guid id)
        {
            var history = await projectService.GetProgressHistoryAsync(id, User);
            return Ok(history);
        }
        
        [HttpPost("progress/sprint")]
        public async Task<IActionResult> CreateFromSprint(CreateSprintProgressDto dto, CancellationToken ct)
        {
            await projectService.CreateFromSprintAsync(dto, ct);
            await milestonesService.EvaluateMilestonesAsync(dto.ProjectId, dto.Words, new CancellationToken());
            return Ok();
        }

    }
}
