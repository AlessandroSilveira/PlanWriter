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
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectController(IProjectService service)
        {
            _service = service;
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            await _service.CreateProjectAsync(dto);
            return Ok(new { Message = "Project created successfully." });
        }
        
        /// <summary>
        /// Adiciona uma entrada de progresso ao projeto.
        /// Add a new progress entry to project
        /// </summary>
        [HttpPost("progress")]
        public async Task<IActionResult> AddProgress([FromBody] AddProjectProgressDto dto)
        {
            await _service.AddProgressAsync(dto);
            return Ok(new { message = "Progress added successfully." });
        }
    }
}