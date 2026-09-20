using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUsuarioAdministrativoAuthenticationService _authenticationService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IUsuarioPermissionRepository _permissionRepository;

    public AuthController(
        IUsuarioAdministrativoAuthenticationService authenticationService,
        IAccessTokenService accessTokenService,
        IUsuarioPermissionRepository permissionRepository)
    {
        _authenticationService = authenticationService;
        _accessTokenService = accessTokenService;
        _permissionRepository = permissionRepository;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var result = await _authenticationService.AuthenticateAsync(request.NombreUsuario, request.Password, cancellationToken);

        if (!result.IsAuthenticated || result.Id is null || string.IsNullOrWhiteSpace(result.NombreUsuario))
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var accessToken = _accessTokenService.Generate(result);
        var permissions = await _permissionRepository.GetPermissionsAsync(result.Id.Value, cancellationToken);

        return Ok(new
        {
            accessToken = accessToken.Token,
            expiresAtUtc = accessToken.ExpiresAtUtc,
            usuario = new
            {
                id = result.Id.Value,
                nombreUsuario = result.NombreUsuario,
                permisos = permissions
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        var nameClaim = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("unique_name");

        if (string.IsNullOrWhiteSpace(idClaim) || string.IsNullOrWhiteSpace(nameClaim) || !int.TryParse(idClaim, out var userId))
        {
            return Unauthorized();
        }

        var permissions = await _permissionRepository.GetPermissionsAsync(userId, cancellationToken);

        return Ok(new
        {
            id = userId,
            nombreUsuario = nameClaim,
            permisos = permissions
        });
    }
}

public sealed class LoginRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
