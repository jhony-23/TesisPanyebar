using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.DashboardReportes;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "Permission:" + AdministrativePermissionCodes.ReportesVer)]
public sealed class ReportesController : ControllerBase
{
    private readonly IDashboardReportesService _service;
    public ReportesController(IDashboardReportesService service) => _service = service;

    [HttpGet("pagos")]
    public async Task<IActionResult> GetPagos([FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta, CancellationToken cancellationToken)
    {
        var result = await _service.GetPagosAsync(fechaDesde, fechaHasta, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "El rango de fechas no es válido." });
    }

    [HttpGet("recaudacion-por-sector")]
    public async Task<IActionResult> GetRecaudacionPorSector([FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta, CancellationToken cancellationToken)
    {
        var result = await _service.GetRecaudacionPorSectorAsync(fechaDesde, fechaHasta, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "El rango de fechas no es válido." });
    }

    [HttpGet("obligaciones-pendientes")]
    public async Task<IActionResult> GetObligacionesPendientes(CancellationToken cancellationToken) =>
        Ok(await _service.GetObligacionesPendientesAsync(cancellationToken));

    [HttpGet("jornadas")]
    public async Task<IActionResult> GetJornadas([FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta, CancellationToken cancellationToken)
    {
        var result = await _service.GetJornadasAsync(fechaDesde, fechaHasta, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = "El rango de fechas no es válido." });
    }
}
