using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.API.Security;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Domain.Dtos.AdminEvents;
using PlanWriter.Domain.Requests;
using CreateEventRequest = PlanWriter.Domain.Dtos.Events.CreateEventRequest;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/admin/events")]
[AdminOnly]
public class AdminEventsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lista eventos ativos (admin também usa)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    => Ok(await mediator.Send(new GetActiveQuery()));
        

    /// <summary>
    /// Detalhe do evento
    /// </summary>
    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetById(Guid eventId)
    {
        var eventDto = await mediator.Send(new GetAdminEventByIdQuery(eventId));
        return eventDto is null
            ? NotFound(new { message = "Evento não encontrado." })
            : Ok(eventDto);
    }
    
    /// <summary>
    /// Detalhe do evento
    /// </summary>
    [HttpGet()]
    public async Task<IActionResult> GetEvents()
    {
        var allEvents = await mediator.Send(new GetAdminEventsQuery());
        return allEvents is null
            ? NotFound(new { message = "Event not found." })
            : Ok(allEvents);
    }

    /// <summary>
    /// Criar novo evento (ADMIN)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminEventRequest req)
    {
        var ev =  await mediator.Send(new CreateAdminEventCommand(req));
         return ev is null 
             ? BadRequest(new { message = "Could not create event." }) : 
             CreatedAtAction(nameof(GetById), new { eventId = ev.Id }, ev);
    }
    
    /// <summary>
    /// Atualizar evento (ADMIN)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventDto request)
    {
       await mediator.Send(new UpdateAdminEventCommand(request, id));
       return NoContent();
    }
    
    /// <summary>
    /// Deletar um evento (ADMIN)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteAdminEventCommand(id));
        return NoContent();
    }
}