using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/admin/events")]
[Authorize(Roles = "Admin")]
public class AdminEventsController : ControllerBase
{
    private readonly IEventService eventService;

    public AdminEventsController(IEventService eventService)
    {
        this.eventService = eventService;
    }

    /// <summary>
    /// Lista eventos ativos (admin também usa)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
        => Ok(await eventService.GetActiveAsync());

    /// <summary>
    /// Detalhe do evento
    /// </summary>
    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetById(Guid eventId)
    {
        var ev = await eventService.GetByIdAsync(eventId);
        return ev is null
            ? NotFound(new { message = "Evento não encontrado." })
            : Ok(ev);
    }

    /// <summary>
    /// Criar novo evento (ADMIN)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest req)
    {
        var ev = await eventService.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { eventId = ev.Id }, ev);
    }
}