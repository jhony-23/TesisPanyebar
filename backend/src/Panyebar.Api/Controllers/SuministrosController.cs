using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Panyebar.Application.Security;
using Panyebar.Application.Suministros;

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

    [HttpGet("{id:int}/procesos")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetProcesos(int id, CancellationToken cancellationToken)
    {
        var procesos = await _suministroService.GetProcesosAsync(id, cancellationToken);
        return procesos is null
            ? NotFound(new { message = "Suministro no encontrado." })
            : Ok(procesos);
    }

    [HttpPost("{id:int}/cancelacion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public Task<IActionResult> Cancel(int id, [FromBody] SuministroProcesoInput request, CancellationToken cancellationToken)
    {
        return ProcessStateChangeAsync(
            id,
            request,
            (usuarioId, token) => _suministroService.CancelAsync(id, request, usuarioId, token),
            cancellationToken);
    }

    [HttpPost("{id:int}/reconexion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public Task<IActionResult> Reconnect(int id, [FromBody] SuministroProcesoInput request, CancellationToken cancellationToken)
    {
        return ProcessStateChangeAsync(
            id,
            request,
            (usuarioId, token) => _suministroService.ReconnectAsync(id, request, usuarioId, token),
            cancellationToken);
    }

    [HttpPut("{id:int}/responsable")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public async Task<IActionResult> SetResponsable(int id, [FromBody] SetResponsableInput request, CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!int.TryParse(claim, out var usuarioAdministrativoId) || usuarioAdministrativoId <= 0)
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await _suministroService.SetResponsableAsync(id, request, usuarioAdministrativoId, cancellationToken);
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

    private async Task<IActionResult> ProcessStateChangeAsync(
        int id,
        SuministroProcesoInput request,
        Func<int, CancellationToken, Task<SuministroOperationResult<SuministroDto>>> operation,
        CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!int.TryParse(claim, out var usuarioAdministrativoId) || usuarioAdministrativoId <= 0)
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await operation(usuarioAdministrativoId, cancellationToken);
        return result.Error switch
        {
            SuministroOperationError.Invalid => BadRequest(new { message = "El motivo es obligatorio y debe respetar las longitudes permitidas." }),
            SuministroOperationError.NotFound => NotFound(new { message = "Suministro no encontrado." }),
            SuministroOperationError.Conflict => Conflict(new { message = "La transición no está permitida para el estado actual del suministro." }),
            _ => Ok(result.Value)
        };
    }
}