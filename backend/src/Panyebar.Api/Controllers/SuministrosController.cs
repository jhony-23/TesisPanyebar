using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Security;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/suministros")]
public sealed class SuministrosController : ControllerBase
{
    private readonly ISuministroService _suministroService;

    public SuministrosController(ISuministroService suministroService)
    {
        _suministroService = suministroService;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _suministroService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var suministro = await _suministroService.GetByIdAsync(id, cancellationToken);
        return suministro is null
            ? NotFound(new { message = "Suministro no encontrado." })
            : Ok(suministro);
    }

    [HttpGet("nis/{nis}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetByNis(string nis, CancellationToken cancellationToken)
    {
        var suministro = await _suministroService.GetByNisAsync(nis, cancellationToken);
        return suministro is null
            ? NotFound(new { message = "Suministro no encontrado." })
            : Ok(suministro);
    }

    [HttpGet("{id:int}/qr")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetQr(int id, CancellationToken cancellationToken)
    {
        var qr = await _suministroService.GetQrAsync(id, cancellationToken);
        return qr is null
            ? NotFound(new { message = "Suministro no encontrado." })
            : Ok(qr);
    }

    [HttpGet("qr/{token}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetByQrToken(string token, CancellationToken cancellationToken)
    {
        var suministro = await _suministroService.GetByQrTokenAsync(token, cancellationToken);
        return suministro is null
            ? NotFound(new { message = "Suministro no encontrado." })
            : Ok(suministro);
    }

    [HttpGet("{id:int}/responsables")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetResponsables(int id, CancellationToken cancellationToken)
    {
        var responsables = await _suministroService.GetResponsablesAsync(id, cancellationToken);
        return responsables is null
            ? NotFound(new { message = "Suministro no encontrado." })
            : Ok(responsables);
    }

    [HttpPut("{id:int}/responsable")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public async Task<IActionResult> SetResponsable(int id, [FromBody] SetResponsableInput request, CancellationToken cancellationToken)
    {
        var result = await _suministroService.SetResponsableAsync(id, request, cancellationToken);
        return result.Error switch
        {
            SuministroOperationError.Invalid => BadRequest(new { message = "La persona debe existir y estar activa." }),
            SuministroOperationError.NotFound => NotFound(new { message = "Suministro no encontrado." }),
            SuministroOperationError.Conflict => Conflict(new { message = "No se pudo establecer el responsable por un conflicto de concurrencia." }),
            _ => Ok(result.Value)
        };
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public async Task<IActionResult> Create([FromBody] SuministroInput request, CancellationToken cancellationToken)
    {
        var result = await _suministroService.CreateAsync(request, cancellationToken);
        return result.Error switch
        {
            SuministroOperationError.Invalid => BadRequest(new { message = "El sector debe existir y estar activo, y la dirección es obligatoria." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
        };
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] SuministroInput request, CancellationToken cancellationToken)
    {
        var result = await _suministroService.UpdateAsync(id, request, cancellationToken);
        return result.Error switch
        {
            SuministroOperationError.Invalid => BadRequest(new { message = "El sector debe existir y estar activo, y la dirección es obligatoria." }),
            SuministroOperationError.NotFound => NotFound(new { message = "Suministro no encontrado." }),
            _ => Ok(result.Value)
        };
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public async Task<IActionResult> SetEstado(int id, [FromBody] UpdateSuministroEstadoRequest request, CancellationToken cancellationToken)
    {
        if (request is null || !Enum.IsDefined(typeof(EstadoSuministro), request.Estado))
        {
            return BadRequest(new { message = "Estado no válido." });
        }

        var result = await _suministroService.SetEstadoAsync(id, request.Estado, cancellationToken);
        return result.Error switch
        {
            SuministroOperationError.Invalid => BadRequest(new { message = "Estado no válido." }),
            SuministroOperationError.NotFound => NotFound(new { message = "Suministro no encontrado." }),
            _ => Ok(result.Value)
        };
    }
}

public sealed class UpdateSuministroEstadoRequest
{
    public EstadoSuministro Estado { get; set; }
}