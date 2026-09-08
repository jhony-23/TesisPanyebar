using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Security;
using Panyebar.Application.SolicitudesNuevoServicio;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/solicitudes-nuevo-servicio")]
public sealed class SolicitudesNuevoServicioController : ControllerBase
{
    private readonly ISolicitudNuevoServicioService _service;

    public SolicitudesNuevoServicioController(ISolicitudNuevoServicioService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosVer)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var request = await _service.GetByIdAsync(id, cancellationToken);
        return request is null
            ? NotFound(new { message = "Solicitud de nuevo servicio no encontrada." })
            : Ok(request);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public async Task<IActionResult> Create(
        [FromBody] SolicitudNuevoServicioInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await _service.CreateAsync(request, userId, cancellationToken);
        return result.Error switch
        {
            SolicitudNuevoServicioOperationError.Invalid => BadRequest(new { message = "La persona, el sector y los datos de la solicitud deben ser válidos." }),
            SolicitudNuevoServicioOperationError.Conflict => Conflict(new { message = "La persona ya tiene una solicitud pendiente de nuevo servicio." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.SolicitudNuevoServicioId }, result.Value)
        };
    }

    [HttpPost("{id:int}/aprobacion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public Task<IActionResult> Approve(
        int id,
        [FromBody] SolicitudNuevoServicioResolutionInput request,
        CancellationToken cancellationToken)
    {
        return ResolveAsync(
            id,
            request,
            (userId, token) => _service.ApproveAsync(id, request, userId, token),
            cancellationToken);
    }

    [HttpPost("{id:int}/rechazo")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SuministrosGestionar)]
    public Task<IActionResult> Reject(
        int id,
        [FromBody] SolicitudNuevoServicioResolutionInput request,
        CancellationToken cancellationToken)
    {
        return ResolveAsync(
            id,
            request,
            (userId, token) => _service.RejectAsync(id, request, userId, token),
            cancellationToken);
    }

    private async Task<IActionResult> ResolveAsync(
        int id,
        SolicitudNuevoServicioResolutionInput request,
        Func<int, CancellationToken, Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>>> operation,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await operation(userId, cancellationToken);
        return result.Error switch
        {
            SolicitudNuevoServicioOperationError.Invalid => BadRequest(new { message = "Los datos de resolución no son válidos." }),
            SolicitudNuevoServicioOperationError.NotFound => NotFound(new { message = "Solicitud de nuevo servicio no encontrada." }),
            SolicitudNuevoServicioOperationError.Conflict => Conflict(new { message = "La solicitud ya fue resuelta." }),
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