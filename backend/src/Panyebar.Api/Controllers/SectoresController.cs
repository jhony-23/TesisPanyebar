using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Sectores;
using Panyebar.Application.Security;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/sectores")]
public sealed class SectoresController : ControllerBase
{
    private readonly ISectorService _sectorService;

    public SectoresController(ISectorService sectorService)
    {
        _sectorService = sectorService;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:SECTORES.VER")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _sectorService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:SECTORES.VER")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var sector = await _sectorService.GetByIdAsync(id, cancellationToken);
        return sector is null
            ? NotFound(new { message = "Sector no encontrado." })
            : Ok(sector);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:SECTORES.GESTIONAR")]
    public async Task<IActionResult> Create([FromBody] SectorInput request, CancellationToken cancellationToken)
    {
        var result = await _sectorService.CreateAsync(request, cancellationToken);
        return result.Error switch
        {
            SectorOperationError.Invalid => BadRequest(new { message = "El nombre y la descripción no tienen un formato válido." }),
            SectorOperationError.Duplicate => Conflict(new { message = "Ya existe un sector con ese nombre." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
        };
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:SECTORES.GESTIONAR")]
    public async Task<IActionResult> Update(int id, [FromBody] SectorInput request, CancellationToken cancellationToken)
    {
        var result = await _sectorService.UpdateAsync(id, request, cancellationToken);
        return result.Error switch
        {
            SectorOperationError.Invalid => BadRequest(new { message = "El nombre y la descripción no tienen un formato válido." }),
            SectorOperationError.NotFound => NotFound(new { message = "Sector no encontrado." }),
            SectorOperationError.Duplicate => Conflict(new { message = "Ya existe un sector con ese nombre." }),
            _ => Ok(result.Value)
        };
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "Permission:SECTORES.GESTIONAR")]
    public async Task<IActionResult> SetEstado(int id, [FromBody] UpdateSectorEstadoRequest request, CancellationToken cancellationToken)
    {
        if (request is null || !Enum.IsDefined(typeof(EstadoRegistro), request.Estado))
        {
            return BadRequest(new { message = "Estado no válido." });
        }

        var result = await _sectorService.SetEstadoAsync(id, request.Estado, cancellationToken);
        return result.Error switch
        {
            SectorOperationError.NotFound => NotFound(new { message = "Sector no encontrado." }),
            SectorOperationError.Invalid => BadRequest(new { message = "Estado no válido." }),
            _ => Ok(result.Value)
        };
    }
}

public sealed class UpdateSectorEstadoRequest
{
    public EstadoRegistro Estado { get; set; }
}
