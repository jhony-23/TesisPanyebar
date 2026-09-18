using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Abastecimiento;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/abastecimiento")]
public sealed class AbastecimientoController : ControllerBase
{
    private readonly IAbastecimientoService _service;

    public AbastecimientoController(IAbastecimientoService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoVer)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] int? sectorId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(
            fechaDesde,
            fechaHasta,
            sectorId,
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : BadRequest(new
            {
                message = "Los filtros de abastecimiento no son válidos."
            });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoVer)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);

        return result is null
            ? NotFound(new { message = "Programación no encontrada." })
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoGestionar)]
    public async Task<IActionResult> Create(
        [FromBody] ProgramacionAbastecimientoInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.CreateAsync(
            request,
            userId,
            cancellationToken);

        return MapMutationResult(result, true);
    }

    [HttpPost("recurrente")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoGestionar)]
    public async Task<IActionResult> CreateRecurring(
        [FromBody] ProgramacionRecurrenteAbastecimientoInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.CreateRecurringAsync(
            request,
            userId,
            cancellationToken);

        return result.Error switch
        {
            AbastecimientoOperationError.Invalid =>
                BadRequest(new
                {
                    message = "La configuración de recurrencia no es válida."
                }),

            AbastecimientoOperationError.SectorNotFound =>
                BadRequest(new
                {
                    message = "El sector seleccionado no existe."
                }),

            AbastecimientoOperationError.SectorInactive =>
                Conflict(new
                {
                    message = "El sector seleccionado está inactivo."
                }),

            AbastecimientoOperationError.Conflict =>
                Conflict(new
                {
                    message = "Una o más fechas recurrentes entran en conflicto con programaciones existentes."
                }),

            _ => Created(
                "/api/abastecimiento",
                result.Value)
        };
    }
    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoGestionar)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ProgramacionAbastecimientoInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.UpdateAsync(
            id,
            request,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    [HttpPost("{id:int}/completado")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoGestionar)]
    public async Task<IActionResult> Complete(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        return MapMutationResult(
            await _service.CompleteAsync(
                id,
                userId,
                cancellationToken));
    }

    [HttpPost("{id:int}/cancelacion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AbastecimientoGestionar)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] ActualizarEstadoAbastecimientoInput? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        return MapMutationResult(
            await _service.CancelAsync(
                id,
                request,
                userId,
                cancellationToken));
    }

    private IActionResult MapMutationResult(
        AbastecimientoOperationResult<ProgramacionAbastecimientoDto> result,
        bool created = false)
    {
        return result.Error switch
        {
            AbastecimientoOperationError.Invalid =>
                BadRequest(new
                {
                    message = "Los datos de la programación no son válidos."
                }),

            AbastecimientoOperationError.NotFound =>
                NotFound(new
                {
                    message = "Programación no encontrada."
                }),

            AbastecimientoOperationError.SectorNotFound =>
                BadRequest(new
                {
                    message = "El sector seleccionado no existe."
                }),

            AbastecimientoOperationError.SectorInactive =>
                Conflict(new
                {
                    message = "El sector seleccionado está inactivo."
                }),

            AbastecimientoOperationError.Conflict =>
                Conflict(new
                {
                    message = "La programación no puede modificarse porque ya finalizó o porque el horario se solapa con otra programación activa del mismo sector."
                }),

            _ when created =>
                CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Value!.Id },
                    result.Value),

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
