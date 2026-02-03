using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController(IMediator mediator, IUserService userService) : ControllerBase
    {
        private Guid UserId => userService.GetUserId(User);

        /// <summary>
        /// Create a new project (JSON). Returns the created project with Id.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var response = await mediator.Send(new CreateProjectCommand(dto, UserId));
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Get all projects of current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await mediator.Send(new GetAllProjectsQuery(UserId));
            return Ok(response);
        }

        /// <summary>
        /// Get project by Id
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await mediator.Send(new GetProjectByIdQuery(id, UserId));
            return Ok(result);
        }

        /// <summary>
        /// Set flexible goal (amount + unit + optional deadline)
        /// </summary>
        [HttpPost("{id:guid}/goal")]
        public async Task<IActionResult> SetGoal(Guid id, [FromBody] SetFlexibleGoalDto request)
        {
            var response = await mediator.Send(new SetGoalProjectCommand(id, UserId, request));
            if (!response) return BadRequest("GoalAmount deve ser >= 0.");
            return Ok(new { message = "Goal set successfully." });
            
        }

        /// <summary>
        /// Add a new progress entry (supports Words/Minutes/Pages)
        /// </summary>
        [HttpPost("{id:guid}/progress")]
        public async Task<IActionResult> AddProgress(Guid id, [FromBody] AddProjectProgressDto request)
        {
            
            var response = await mediator.Send(new AddProjectProgressCommand(id, request, UserId));
            if (!response) return BadRequest("ProgressAmount deve ser >= 0.");
            return Ok(new { message = "Progress added successfully." });
        }

        /// <summary>
        /// Get project progress history
        /// </summary>
        [HttpGet("{projectId:guid}/progress/history")]
        public async Task<IActionResult> GetProgressHistory(Guid projectId, CancellationToken ct)
        {
            var userId = userService.GetUserId(User); 
            var result = await mediator.Send(new GetProjectProgressHistoryQuery(projectId, userId), ct);
            return Ok(result);
        }

        /// <summary>
        /// Delete a progress entry
        /// </summary>
        [HttpDelete("progress/{progressId:guid}")]
        public async Task<IActionResult> DeleteProgress(Guid progressId)
        {
            var response = await mediator.Send(new DeleteProgressCommand(progressId, UserId));
            if (!response) return NotFound(new { message = "Progress not found or not authorized." });
            return NoContent();
        }

        /// <summary>
        /// Project stats
        /// </summary>
        [HttpGet("{id:guid}/stats")]
        public async Task<ActionResult<ProjectStatsDto>> GetStats(Guid id)
        {
            var response = await mediator.Send(new GetProjectStatsQuery(id, UserId));
            return Ok(response);
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        [HttpDelete("{projectId:guid}")]
        public async Task<IActionResult> Delete(Guid projectId)
        {
            var response = await mediator.Send(new DeleteProjectCommand(projectId, UserId));
            if (!response) return NotFound(new { message = "Project not found or not authorized." });
            return Ok(new { message = "Project deleted successfully." });
        }

        [HttpGet("{projectId}/history")]
        public async Task<IActionResult> GetHistory(Guid projectId)
        {
            var response = await mediator.Send(new GetProjectProgressHistoryQuery(projectId, UserId));
            return Ok(response);
        }
        
        [HttpPost("progress/sprint")]
        public async Task<IActionResult> CreateFromSprint(CreateSprintProgressDto dto)
        {
            await mediator.Send(new AddProjectProgressCommand(
                dto.ProjectId,
               new AddProjectProgressDto
                {
                    ProjectId = dto.ProjectId,
                    WordsWritten = dto.Words,
                    Minutes = dto.Minutes,
                    Date = dto.Date,
                    Notes = $"Word Sprint — {dto.Words} palavras em {dto.Minutes} min"
                },
                UserId
            ));

            return Ok();
        }

        
        [Authorize]
        [HttpGet("monthly")]
        public async Task<IActionResult> MonthlyProgress()
        {
            var response =  await mediator.Send(new GetMonthlyProgressQuery(UserId));
            
            return Ok(new { response, month = DateTime.UtcNow.ToString("yyyy-MM") });
        }


    }
}
