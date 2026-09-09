using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Obligaciones;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/obligaciones")]
public sealed class ObligacionesController : ControllerBase
{
    private readonly IObligacionService _service;

    public ObligacionesController(IObligacionService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.ObligacionesVer)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.ObligacionesVer)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var obligation = await _service.GetByIdAsync(id, cancellationToken);
        return obligation is null
            ? NotFound(new { message = "Obligación no encontrada." })
            : Ok(obligation);
    }

    [HttpPost("desde-cuota")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.ObligacionesGestionar)]
    public async Task<IActionResult> GenerateFromCuota(
        [FromBody] GenerarObligacionCuotaInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await _service.GenerateFromCuotaAsync(request, userId, cancellationToken);
        return result.Error switch
        {
            ObligacionOperationError.Invalid => BadRequest(new { message = "Los datos o el período de la obligación no son válidos." }),
            ObligacionOperationError.NotFound => NotFound(new { message = "La cuota o el suministro no existen." }),
            ObligacionOperationError.Conflict => Conflict(new { message = "Ya existe una obligación no anulada para la cuota, suministro y período." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
        };
    }

    [HttpPost("{id:int}/anulacion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.ObligacionesGestionar)]
    public async Task<IActionResult> Annul(
        int id,
        [FromBody] AnularObligacionInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await _service.AnnulAsync(id, request, userId, cancellationToken);
        return result.Error switch
        {
            ObligacionOperationError.Invalid => BadRequest(new { message = "El motivo de anulación es obligatorio." }),
            ObligacionOperationError.NotFound => NotFound(new { message = "Obligación no encontrada." }),
            ObligacionOperationError.Conflict => Conflict(new { message = "Solo una obligación pendiente puede anularse." }),
            _ => Ok(result.Value)
        };
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(claim, out userId) && userId > 0;
    }
}
