using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    // lista eventos ativos
    [HttpGet("active")]
    public async Task<ActionResult<EventDto[]>> GetActive()
        => Ok(await eventService.GetActiveAsync());

    // ✅ novo: detalhe do evento
    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid eventId)
    {
        var ev = await eventService.GetByIdAsync(eventId);
        return ev is null ? NotFound(new { message = "Evento não encontrado." }) : Ok(ev);
    }

    // inscrever/atualizar meta (upsert)
    [Authorize]
    [HttpPost("join")]
    public async Task<ActionResult> Join([FromBody] JoinEventRequest req)
    {
        var pe = await eventService.JoinAsync(req);
        return Ok(new { pe.Id, pe.ProjectId, pe.EventId, pe.TargetWords });
    }

    // progresso do projeto dentro do evento
    [Authorize]
    [HttpGet("{eventId:guid}/projects/{projectId:guid}/progress")]
    public async Task<ActionResult<EventProgressDto>> GetProgress(Guid eventId, Guid projectId)
    {
        var dto = await eventService.GetProgressAsync(projectId, eventId);
        return Ok(dto);
    }

    // finalizar (winner/participant) — mantém para compatibilidade
    [Authorize]
    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize([FromBody] FinalizeRequest req)
    {
        var pe = await eventService.FinalizeAsync(req.ProjectEventId);
        return Ok(new { pe.Id, pe.Won, pe.FinalWordCount, pe.ValidatedAtUtc });
    }

    // ✅ novo: sair do evento
    [Authorize]
    [HttpDelete("{eventId:guid}/projects/{projectId:guid}")]
    public async Task<IActionResult> Leave(Guid eventId, Guid projectId)
    {
        await eventService.LeaveAsync(projectId, eventId);
        return NoContent();
    }

    // leaderboard
    [HttpGet("{eventId:guid}/leaderboard")]
    public async Task<IActionResult> Leaderboard(Guid eventId, [FromQuery] string scope = "total",
        [FromQuery] int top = 50)
    {
        var response = await eventService.GetLeaberBoard(eventId, scope, top);
        return Ok(response);
    }
}