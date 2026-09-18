using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Security;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdministrativeAccessController : ControllerBase
{
    private readonly IAdministrativeAccessService _service;

    public AdministrativeAccessController(
        IAdministrativeAccessService service)
    {
        _service = service;
    }

    [HttpGet("usuarios")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.UsuariosVer)]
    public async Task<IActionResult> GetUsuarios(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetUsuariosAsync(cancellationToken));

    [HttpGet("usuarios/{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.UsuariosVer)]
    public async Task<IActionResult> GetUsuarioById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetUsuarioByIdAsync(id, cancellationToken);

        return result is null
            ? NotFound(new { message = "Usuario no encontrado." })
            : Ok(result);
    }

    [HttpPost("usuarios")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.UsuariosGestionar)]
    public async Task<IActionResult> CreateUsuario(
        [FromBody] CreateUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.CreateUsuarioAsync(
            request?.NombreUsuario ?? string.Empty,
            request?.Password ?? string.Empty,
            actorId,
            cancellationToken);

        return result.Error switch
        {
            AdministrativeAccessError.Invalid =>
                BadRequest(new { message = "El nombre de usuario o la contraseña no son válidos. La contraseña debe tener al menos 8 caracteres." }),

            AdministrativeAccessError.Duplicate =>
                Conflict(new { message = "Ya existe un usuario con ese nombre." }),

            _ => CreatedAtAction(
                nameof(GetUsuarioById),
                new { id = result.Value!.Id },
                result.Value)
        };
    }

    [HttpPatch("usuarios/{id:int}/estado")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.UsuariosGestionar)]
    public async Task<IActionResult> SetUsuarioEstado(
        int id,
        [FromBody] UpdateEstadoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.SetUsuarioEstadoAsync(
            id,
            request.Estado,
            actorId,
            cancellationToken);

        return MapUsuarioResult(result,
            "No puedes desactivar tu propia cuenta ni dejar el sistema sin un administrador funcional.");
    }

    [HttpPut("usuarios/{id:int}/roles")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.UsuariosGestionar)]
    public async Task<IActionResult> SetUsuarioRoles(
        int id,
        [FromBody] UpdateRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.SetRolesForUsuarioAsync(
            id,
            request?.RoleIds ?? Array.Empty<int>(),
            actorId,
            cancellationToken);

        return MapUsuarioResult(result,
            "La asignación dejaría el sistema sin un administrador funcional.");
    }

    [HttpPut("usuarios/{id:int}/password")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.UsuariosGestionar)]
    public async Task<IActionResult> ResetUsuarioPassword(
        int id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.ResetUsuarioPasswordAsync(
            id,
            request?.NuevaPassword ?? string.Empty,
            actorId,
            cancellationToken);

        return MapUsuarioResult(
            result,
            "No se pudo restablecer la contraseña.");
    }

    [HttpGet("roles")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.RolesVer)]
    public async Task<IActionResult> GetRoles(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetRolesAsync(cancellationToken));

    [HttpGet("roles/{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.RolesVer)]
    public async Task<IActionResult> GetRolById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetRolByIdAsync(id, cancellationToken);

        return result is null
            ? NotFound(new { message = "Rol no encontrado." })
            : Ok(result);
    }

    [HttpPost("roles")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.RolesGestionar)]
    public async Task<IActionResult> CreateRol(
        [FromBody] CreateRolRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.CreateRolAsync(
            request?.Nombre ?? string.Empty,
            request?.Descripcion,
            actorId,
            cancellationToken);

        return result.Error switch
        {
            AdministrativeAccessError.Invalid =>
                BadRequest(new { message = "Los datos del rol no son válidos." }),

            AdministrativeAccessError.Duplicate =>
                Conflict(new { message = "Ya existe un rol con ese nombre." }),

            _ => CreatedAtAction(
                nameof(GetRolById),
                new { id = result.Value!.Id },
                result.Value)
        };
    }

    [HttpPatch("roles/{id:int}/estado")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.RolesGestionar)]
    public async Task<IActionResult> SetRolEstado(
        int id,
        [FromBody] UpdateEstadoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.SetRolEstadoAsync(
            id,
            request.Estado,
            actorId,
            cancellationToken);

        return MapRolResult(
            result,
            "El cambio dejaría el sistema sin un administrador funcional.");
    }

    [HttpPut("roles/{id:int}/permisos")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PermisosAsignar)]
    public async Task<IActionResult> SetPermisosForRol(
        int id,
        [FromBody] UpdatePermisosRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario autenticado." });
        }

        var result = await _service.SetPermisosForRolAsync(
            id,
            request?.PermisoIds ?? Array.Empty<int>(),
            actorId,
            cancellationToken);

        return MapRolResult(
            result,
            "La asignación dejaría el sistema sin un administrador funcional.");
    }

    [HttpGet("permisos")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PermisosVer)]
    public async Task<IActionResult> GetPermisos(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetPermisosAsync(cancellationToken));

    [HttpGet("permisos/{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PermisosVer)]
    public async Task<IActionResult> GetPermisoById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetPermisoByIdAsync(id, cancellationToken);

        return result is null
            ? NotFound(new { message = "Permiso no encontrado." })
            : Ok(result);
    }

    private IActionResult MapUsuarioResult(
        AdministrativeAccessResult<UsuarioAdministrativoAccessSummary> result,
        string conflictMessage)
    {
        return result.Error switch
        {
            AdministrativeAccessError.Invalid =>
                BadRequest(new { message = "Los datos de la operación no son válidos." }),

            AdministrativeAccessError.NotFound =>
                NotFound(new { message = "Usuario no encontrado." }),

            AdministrativeAccessError.Conflict =>
                Conflict(new { message = conflictMessage }),

            _ => Ok(result.Value)
        };
    }

    private IActionResult MapRolResult(
        AdministrativeAccessResult<RolAccessSummary> result,
        string conflictMessage)
    {
        return result.Error switch
        {
            AdministrativeAccessError.Invalid =>
                BadRequest(new { message = "Los datos de la operación no son válidos." }),

            AdministrativeAccessError.NotFound =>
                NotFound(new { message = "Rol no encontrado." }),

            AdministrativeAccessError.Conflict =>
                Conflict(new { message = conflictMessage }),

            _ => Ok(result.Value)
        };
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(claim, out userId) && userId > 0;
    }

    [HttpGet("auditoria")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.SeguridadAuditoriaVer)]
    public async Task<IActionResult> GetAuditoria(
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? usuarioId,
        [FromQuery] string? accion,
        [FromQuery] string? entidad,
        [FromQuery] int limite = 200,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId.HasValue && usuarioId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "El usuario indicado no es válido."
            });
        }

        if (limite < 1 || limite > 500)
        {
            return BadRequest(new
            {
                message = "El límite debe estar entre 1 y 500."
            });
        }

        if (fechaDesde.HasValue &&
            fechaHasta.HasValue &&
            fechaDesde.Value > fechaHasta.Value)
        {
            return BadRequest(new
            {
                message = "El rango de fechas no es válido."
            });
        }

        DateTime? fromUtc = fechaDesde.HasValue
            ? NormalizeUtc(fechaDesde.Value)
            : null;

        DateTime? untilUtcExclusive = fechaHasta.HasValue
            ? NormalizeUtc(fechaHasta.Value).AddTicks(1)
            : null;

        return Ok(
            await _service.GetAuditoriaAsync(
                fromUtc,
                untilUtcExclusive,
                usuarioId,
                accion,
                entidad,
                limite,
                cancellationToken));
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}

public sealed class CreateUsuarioRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateEstadoRequest
{
    public EstadoRegistro Estado { get; set; }
}

public sealed class UpdateRolesRequest
{
    public IEnumerable<int> RoleIds { get; set; } = Array.Empty<int>();
}

public sealed class ResetPasswordRequest
{
    public string NuevaPassword { get; set; } = string.Empty;
}

public sealed class CreateRolRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public sealed class UpdatePermisosRequest
{
    public IEnumerable<int> PermisoIds { get; set; } = Array.Empty<int>();



}
