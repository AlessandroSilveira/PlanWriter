using Microsoft.AspNetCore.Mvc;
using PlanWriter.API.Security;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Domain.Requests;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/admin/events")]
[AdminOnly]
public class AdminEventsController(IEventService eventService) : ControllerBase
{
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
    /// Detalhe do evento
    /// </summary>
    [HttpGet()]
    public async Task<IActionResult> GetEvents()
    {
        var ev = await eventService.GetAllAsync();
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
    
    /// <summary>
    /// Atualizar evento (ADMIN)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventDto dto)
    {
        await eventService.UpdateAsync(id, dto);
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await eventService.DeleteAsync(id);
        return NoContent();
    }
}