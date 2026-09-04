using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Personas;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/personas")]
public sealed class PersonasController : ControllerBase
{
    private readonly IPersonaService _personaService;

    public PersonasController(IPersonaService personaService)
    {
        _personaService = personaService;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:PERSONAS.VER")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await _personaService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:PERSONAS.VER")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var persona = await _personaService.GetByIdAsync(id, cancellationToken);
        return persona is null
            ? NotFound(new { message = "Persona no encontrada." })
            : Ok(persona);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:PERSONAS.GESTIONAR")]
    public async Task<IActionResult> Create([FromBody] PersonaInput request, CancellationToken cancellationToken)
    {
        var result = await _personaService.CreateAsync(request, cancellationToken);
        return result.Error switch
        {
            PersonaOperationError.Invalid => BadRequest(new { message = "Los datos de la persona no son válidos." }),
            PersonaOperationError.Duplicate => Conflict(new { message = "Ya existe una persona con esa identificación." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
        };
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:PERSONAS.GESTIONAR")]
    public async Task<IActionResult> Update(int id, [FromBody] PersonaInput request, CancellationToken cancellationToken)
    {
        var result = await _personaService.UpdateAsync(id, request, cancellationToken);
        return result.Error switch
        {
            PersonaOperationError.Invalid => BadRequest(new { message = "Los datos de la persona no son válidos." }),
            PersonaOperationError.NotFound => NotFound(new { message = "Persona no encontrada." }),
            PersonaOperationError.Duplicate => Conflict(new { message = "Ya existe una persona con esa identificación." }),
            _ => Ok(result.Value)
        };
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "Permission:PERSONAS.GESTIONAR")]
    public async Task<IActionResult> SetEstado(int id, [FromBody] UpdatePersonaEstadoRequest request, CancellationToken cancellationToken)
    {
        if (request is null || !Enum.IsDefined(typeof(EstadoRegistro), request.Estado))
        {
            return BadRequest(new { message = "Estado no válido." });
        }

        var result = await _personaService.SetEstadoAsync(id, request.Estado, cancellationToken);
        return result.Error switch
        {
            PersonaOperationError.Invalid => BadRequest(new { message = "Estado no válido." }),
            PersonaOperationError.NotFound => NotFound(new { message = "Persona no encontrada." }),
            _ => Ok(result.Value)
        };
    }
}

public sealed class UpdatePersonaEstadoRequest
{
    public EstadoRegistro Estado { get; set; }
}
