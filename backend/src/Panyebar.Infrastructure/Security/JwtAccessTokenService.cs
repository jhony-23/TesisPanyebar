using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Panyebar.Application.Security;

namespace Panyebar.Infrastructure.Security;

public sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly JwtTokenOptions _options;

    public JwtAccessTokenService(IOptions<JwtTokenOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Issuer))
        {
            throw new InvalidOperationException("La configuración JWT 'Issuer' es requerida.");
        }

        if (string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException("La configuración JWT 'Audience' es requerida.");
        }

        if (string.IsNullOrWhiteSpace(_options.Key))
        {
            throw new InvalidOperationException("La configuración JWT 'Key' es requerida.");
        }
    }

    public AccessTokenResult Generate(UsuarioAdministrativoAuthenticationResult usuario)
    {
        if (!usuario.IsAuthenticated || usuario.Id is null || string.IsNullOrWhiteSpace(usuario.NombreUsuario))
        {
            throw new InvalidOperationException("No se puede generar un token para una identidad no autenticada.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.Value.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();

        return new AccessTokenResult
        {
            Token = handler.WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
