using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Finanzas;
using Panyebar.Application.Security;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/egresos")]
public sealed class EgresosController : ControllerBase
{
    private readonly IFinanzaService _service;
    public EgresosController(IFinanzaService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasVer)]
    public async Task<IActionResult> GetAll([FromQuery] DateOnly? fechaDesde, [FromQuery] DateOnly? fechaHasta,
        [FromQuery] EstadoEgreso? estado, CancellationToken cancellationToken)
    {
        var result = await _service.GetEgresosAsync(fechaDesde, fechaHasta, estado, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "Los filtros no son válidos." });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasVer)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetEgresoByIdAsync(id, cancellationToken);
        return result is null ? NotFound(new { message = "Egreso no encontrado." }) : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasGestionar)]
    public async Task<IActionResult> Register([FromBody] EgresoInput request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId)) return Unauthorized();
        var result = await _service.RegisterAsync(request, userId, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : OperationResponse(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] EgresoInput request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId)) return Unauthorized();
        return OperationResponse(await _service.UpdateAsync(id, request, userId, cancellationToken));
    }

    [HttpPost("{id:int}/anulacion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasGestionar)]
    public async Task<IActionResult> Annul(int id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId)) return Unauthorized();
        return OperationResponse(await _service.AnnulAsync(id, userId, cancellationToken));
    }

    private IActionResult OperationResponse(FinanzaOperationResult<EgresoDto> result) => result.Error switch
    {
        FinanzaOperationError.Invalid => BadRequest(new { message = "Los datos del egreso no son válidos." }),
        FinanzaOperationError.NotFound => NotFound(new { message = "Egreso no encontrado." }),
        FinanzaOperationError.Conflict => Conflict(new { message = "El egreso fue modificado o está anulado. Actualiza la consulta." }),
        _ => Ok(result.Value)
    };

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(claim, out userId) && userId > 0 && User.Identity?.IsAuthenticated == true;
    }
}
