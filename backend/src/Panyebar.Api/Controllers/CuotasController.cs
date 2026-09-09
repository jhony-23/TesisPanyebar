using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Cuotas;
using Panyebar.Application.Security;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/cuotas")]
public sealed class CuotasController : ControllerBase
{
    private readonly ICuotaService _service;

    public CuotasController(ICuotaService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.CuotasVer)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.CuotasVer)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var fee = await _service.GetByIdAsync(id, cancellationToken);
        return fee is null
            ? NotFound(new { message = "Cuota no encontrada." })
            : Ok(fee);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.CuotasGestionar)]
    public async Task<IActionResult> Create([FromBody] CuotaInput request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await _service.CreateAsync(request, userId, cancellationToken);
        return result.Error switch
        {
            CuotaOperationError.Invalid => BadRequest(new { message = "Los datos de la cuota no son válidos." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
        };
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.CuotasGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] CuotaInput request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        var result = await _service.UpdateAsync(id, request, userId, cancellationToken);
        return result.Error switch
        {
            CuotaOperationError.Invalid => BadRequest(new { message = "Los datos de la cuota no son válidos." }),
            CuotaOperationError.NotFound => NotFound(new { message = "Cuota no encontrada." }),
            _ => Ok(result.Value)
        };
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.CuotasGestionar)]
    public async Task<IActionResult> SetEstado(
        int id,
        [FromBody] UpdateCuotaEstadoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario administrativo autenticado." });
        }

        if (request is null)
        {
            return BadRequest(new { message = "El estado no es válido." });
        }

        var result = await _service.SetEstadoAsync(id, request.Estado, userId, cancellationToken);
        return result.Error switch
        {
            CuotaOperationError.Invalid => BadRequest(new { message = "El estado no es válido." }),
            CuotaOperationError.NotFound => NotFound(new { message = "Cuota no encontrada." }),
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

public sealed class UpdateCuotaEstadoRequest
{
    public EstadoRegistro Estado { get; set; }
}
