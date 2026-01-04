using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.API.Security;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/admin/events")]
[AdminOnly]
public class AdminEventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public AdminEventsController(IEventService eventService)
    {
        this._eventService = eventService;
    }

    /// <summary>
    /// Lista eventos ativos (admin também usa)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
        => Ok(await _eventService.GetActiveAsync());

    /// <summary>
    /// Detalhe do evento
    /// </summary>
    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetById(Guid eventId)
    {
        var ev = await _eventService.GetByIdAsync(eventId);
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
        var ev = await _eventService.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { eventId = ev.Id }, ev);
    }
}