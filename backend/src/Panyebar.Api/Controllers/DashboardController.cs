using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.DashboardReportes;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "Permission:" + AdministrativePermissionCodes.DashboardVer)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardReportesService _service;
    public DashboardController(IDashboardReportesService service) => _service = service;

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen([FromQuery] int anio, [FromQuery] int mes,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetResumenAsync(anio, mes, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new
        {
            message = "Indique un año entre 1 y 9999 y un mes entre 1 y 12; diciembre de 9999 no admite un intervalo mensual representable."
        });
    }
}
