using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Finanzas;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/finanzas")]
public sealed class FinanzasController : ControllerBase
{
    private readonly IFinanzaService _service;
    public FinanzasController(IFinanzaService service) => _service = service;

    [HttpGet("ingresos")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasVer)]
    public async Task<IActionResult> GetIngresos([FromQuery] DateOnly? fechaDesde, [FromQuery] DateOnly? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetIngresosAsync(fechaDesde, fechaHasta, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "El rango de fechas no es válido." });
    }

    [HttpGet("movimientos")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasVer)]
    public async Task<IActionResult> GetMovimientos([FromQuery] DateOnly? fechaDesde, [FromQuery] DateOnly? fechaHasta,
        [FromQuery] TipoMovimientoFinanciero? tipo, CancellationToken cancellationToken)
    {
        var result = await _service.GetMovimientosAsync(fechaDesde, fechaHasta, tipo, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "Los filtros no son válidos." });
    }

    [HttpGet("resumen")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.FinanzasVer)]
    public async Task<IActionResult> GetResumen([FromQuery] DateOnly? fechaDesde, [FromQuery] DateOnly? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetResumenAsync(fechaDesde, fechaHasta, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "El rango de fechas no es válido." });
    }
}
