using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Security;
using Panyebar.Domain.Enums;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdministrativeAccessController : ControllerBase
{
    private readonly IAdministrativeAccessService _administrativeAccessService;

    public AdministrativeAccessController(IAdministrativeAccessService administrativeAccessService)
    {
        _administrativeAccessService = administrativeAccessService;
    }

    [HttpGet("usuarios")]
    [Authorize(Policy = "Permission:SEGURIDAD.USUARIOS.VER")]
    public async Task<IActionResult> GetUsuarios(CancellationToken cancellationToken)
    {
        var usuarios = await _administrativeAccessService.GetUsuariosAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("usuarios/{id:int}")]
    [Authorize(Policy = "Permission:SEGURIDAD.USUARIOS.VER")]
    public async Task<IActionResult> GetUsuarioById(int id, CancellationToken cancellationToken)
    {
        var usuario = await _administrativeAccessService.GetUsuarioByIdAsync(id, cancellationToken);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost("usuarios")]
    [Authorize(Policy = "Permission:SEGURIDAD.USUARIOS.GESTIONAR")]
    public async Task<IActionResult> CreateUsuario([FromBody] CreateUsuarioRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Nombre de usuario y contraseña son requeridos." });
        }

        var usuario = await _administrativeAccessService.CreateUsuarioAsync(request.NombreUsuario, request.Password, cancellationToken);

        if (usuario is null)
        {
            return Conflict(new { message = "El usuario ya existe o los datos son inválidos." });
        }

        return CreatedAtAction(nameof(GetUsuarioById), new { id = usuario.Id }, usuario);
    }

    [HttpPatch("usuarios/{id:int}/estado")]
    [Authorize(Policy = "Permission:SEGURIDAD.USUARIOS.GESTIONAR")]
    public async Task<IActionResult> SetUsuarioEstado(int id, [FromBody] UpdateEstadoRequest request, CancellationToken cancellationToken)
    {
        if (request is null || !Enum.IsDefined(typeof(EstadoRegistro), request.Estado))
        {
            return BadRequest(new { message = "Estado no válido." });
        }

        var usuario = await _administrativeAccessService.GetUsuarioByIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        var success = await _administrativeAccessService.SetUsuarioEstadoAsync(id, request.Estado, cancellationToken);
        return success ? Ok(new { message = "Estado actualizado correctamente." }) : BadRequest(new { message = "No se pudo actualizar el estado del usuario." });
    }

    [HttpPut("usuarios/{id:int}/roles")]
    [Authorize(Policy = "Permission:SEGURIDAD.USUARIOS.GESTIONAR")]
    public async Task<IActionResult> SetUsuarioRoles(int id, [FromBody] UpdateRolesRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "La asignación de roles es requerida." });
        }

        var usuario = await _administrativeAccessService.GetUsuarioByIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        var success = await _administrativeAccessService.SetRolesForUsuarioAsync(id, request.RoleIds, cancellationToken);
        return success ? Ok(new { message = "Roles actualizados correctamente." }) : BadRequest(new { message = "No se pudieron actualizar los roles." });
    }

    [HttpGet("roles")]
    [Authorize(Policy = "Permission:SEGURIDAD.ROLES.VER")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _administrativeAccessService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("roles/{id:int}")]
    [Authorize(Policy = "Permission:SEGURIDAD.ROLES.VER")]
    public async Task<IActionResult> GetRolById(int id, CancellationToken cancellationToken)
    {
        var rol = await _administrativeAccessService.GetRolByIdAsync(id, cancellationToken);
        return rol is null ? NotFound() : Ok(rol);
    }

    [HttpPost("roles")]
    [Authorize(Policy = "Permission:SEGURIDAD.ROLES.GESTIONAR")]
    public async Task<IActionResult> CreateRol([FromBody] CreateRolRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { message = "El nombre del rol es requerido." });
        }

        var rol = await _administrativeAccessService.CreateRolAsync(request.Nombre, request.Descripcion, cancellationToken);

        if (rol is null)
        {
            return Conflict(new { message = "El rol ya existe o los datos son inválidos." });
        }

        return CreatedAtAction(nameof(GetRolById), new { id = rol.Id }, rol);
    }

    [HttpPatch("roles/{id:int}/estado")]
    [Authorize(Policy = "Permission:SEGURIDAD.ROLES.GESTIONAR")]
    public async Task<IActionResult> SetRolEstado(int id, [FromBody] UpdateEstadoRequest request, CancellationToken cancellationToken)
    {
        if (request is null || !Enum.IsDefined(typeof(EstadoRegistro), request.Estado))
        {
            return BadRequest(new { message = "Estado no válido." });
        }

        var rol = await _administrativeAccessService.GetRolByIdAsync(id, cancellationToken);
        if (rol is null)
        {
            return NotFound(new { message = "Rol no encontrado." });
        }

        var success = await _administrativeAccessService.SetRolEstadoAsync(id, request.Estado, cancellationToken);
        return success ? Ok(new { message = "Estado actualizado correctamente." }) : BadRequest(new { message = "No se pudo actualizar el estado del rol." });
    }

    [HttpPut("roles/{id:int}/permisos")]
    [Authorize(Policy = "Permission:SEGURIDAD.PERMISOS.ASIGNAR")]
    public async Task<IActionResult> SetPermisosForRol(int id, [FromBody] UpdatePermisosRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "La asignación de permisos es requerida." });
        }

        var rol = await _administrativeAccessService.GetRolByIdAsync(id, cancellationToken);
        if (rol is null)
        {
            return NotFound(new { message = "Rol no encontrado." });
        }

        var success = await _administrativeAccessService.SetPermisosForRolAsync(id, request.PermisoIds, cancellationToken);
        return success ? Ok(new { message = "Permisos actualizados correctamente." }) : BadRequest(new { message = "No se pudieron actualizar los permisos." });
    }

    [HttpGet("permisos")]
    [Authorize(Policy = "Permission:SEGURIDAD.PERMISOS.VER")]
    public async Task<IActionResult> GetPermisos(CancellationToken cancellationToken)
    {
        var permisos = await _administrativeAccessService.GetPermisosAsync(cancellationToken);
        return Ok(permisos);
    }

    [HttpGet("permisos/{id:int}")]
    [Authorize(Policy = "Permission:SEGURIDAD.PERMISOS.VER")]
    public async Task<IActionResult> GetPermisoById(int id, CancellationToken cancellationToken)
    {
        var permiso = await _administrativeAccessService.GetPermisoByIdAsync(id, cancellationToken);
        return permiso is null ? NotFound() : Ok(permiso);
    }
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
    public List<int> RoleIds { get; set; } = new();
}

public sealed class CreateRolRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public sealed class UpdatePermisosRequest
{
    public List<int> PermisoIds { get; set; } = new();
}
