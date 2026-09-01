using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public class PasswordHashingSecurityTests
{
    private readonly IPasswordHashService _passwordHashService = new PasswordHasherAdapter();

    [Fact]
    public void JwtAccessTokenService_GeneratesValidToken()
    {
        var options = Options.Create(new JwtTokenOptions
        {
            Issuer = "Panyebar",
            Audience = "PanyebarClients",
            Key = "this-is-a-test-key-for-jwt-validation-1234567890",
            ExpirationMinutes = 60
        });

        var service = new JwtAccessTokenService(options);
        var result = service.Generate(UsuarioAdministrativoAuthenticationResult.Success(7, "admin"));

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(result.Token);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("7", token.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("admin", token.Claims.First(x => x.Type == ClaimTypes.Name).Value);
        Assert.True(result.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void JwtAccessTokenService_ValidatesSignedTokenWithExpectedConfiguration()
    {
        var options = Options.Create(new JwtTokenOptions
        {
            Issuer = "Panyebar",
            Audience = "PanyebarClients",
            Key = "this-is-a-test-key-for-jwt-validation-1234567890",
            ExpirationMinutes = 60
        });

        var service = new JwtAccessTokenService(options);
        var token = service.Generate(UsuarioAdministrativoAuthenticationResult.Success(11, "usuario"));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Value.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)),
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token.Token, validationParameters, out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal("11", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("usuario", principal.FindFirstValue(ClaimTypes.Name));
    }

    [Fact]
    public void JwtAccessTokenService_Throws_WhenJwtConfigurationIsMissing()
    {
        var options = Options.Create(new JwtTokenOptions
        {
            Issuer = "",
            Audience = "",
            Key = "",
            ExpirationMinutes = 60
        });

        Assert.Throws<InvalidOperationException>(() => new JwtAccessTokenService(options));
    }

    [Fact]
    public void Hash_CreatesSecureHash()
    {
        var password = "MiPassword123!";

        var hash = _passwordHashService.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
        Assert.NotEqual(hash, _passwordHashService.Hash(password));
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var password = "MiPassword123!";
        var hash = _passwordHashService.Hash(password);

        var result = _passwordHashService.Verify(hash, password);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectPassword()
    {
        var password = "MiPassword123!";
        var hash = _passwordHashService.Hash(password);

        var result = _passwordHashService.Verify(hash, "OtraPassword123!");

        Assert.False(result);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsFailure_ForUserNotFound()
    {
        var service = new UsuarioAdministrativoAuthenticationService(
            new InMemoryAuthenticationRepository(null),
            _passwordHashService);

        var result = await service.AuthenticateAsync("noexiste", "Password123!");

        Assert.False(result.IsAuthenticated);
        Assert.Null(result.Id);
        Assert.Null(result.NombreUsuario);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsFailure_ForInactiveUser()
    {
        var storedUser = new UsuarioAdministrativo
        {
            Id = 8,
            NombreUsuario = "admin",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Inactivo
        };

        var service = new UsuarioAdministrativoAuthenticationService(
            new InMemoryAuthenticationRepository(storedUser),
            _passwordHashService);

        var result = await service.AuthenticateAsync("admin", "Password123!");

        Assert.False(result.IsAuthenticated);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsSuccess_ForActiveUserWithCorrectPassword()
    {
        var storedUser = new UsuarioAdministrativo
        {
            Id = 7,
            NombreUsuario = "admin",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var service = new UsuarioAdministrativoAuthenticationService(
            new InMemoryAuthenticationRepository(storedUser),
            _passwordHashService);

        var result = await service.AuthenticateAsync("admin", "Password123!");

        Assert.True(result.IsAuthenticated);
        Assert.Equal(7, result.Id);
        Assert.Equal("admin", result.NombreUsuario);
        Assert.Null(typeof(UsuarioAdministrativoAuthenticationResult).GetProperty("PasswordHash"));
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsFailure_ForIncorrectPassword()
    {
        var storedUser = new UsuarioAdministrativo
        {
            Id = 9,
            NombreUsuario = "admin",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var service = new UsuarioAdministrativoAuthenticationService(
            new InMemoryAuthenticationRepository(storedUser),
            _passwordHashService);

        var result = await service.AuthenticateAsync("admin", "PasswordErronea");

        Assert.False(result.IsAuthenticated);
    }

    private sealed class InMemoryAuthenticationRepository : IUsuarioAdministrativoAuthenticationRepository
    {
        private readonly UsuarioAdministrativo? _user;

        public InMemoryAuthenticationRepository(UsuarioAdministrativo? user)
        {
            _user = user;
        }

        public Task<UsuarioAdministrativo?> GetActiveByNombreUsuarioAsync(
            string nombreUsuario,
            CancellationToken cancellationToken = default)
        {
            if (_user is null)
            {
                return Task.FromResult<UsuarioAdministrativo?>(null);
            }

            if (_user.NombreUsuario == nombreUsuario && _user.Estado == EstadoRegistro.Activo)
            {
                return Task.FromResult<UsuarioAdministrativo?>(_user);
            }

            return Task.FromResult<UsuarioAdministrativo?>(null);
        }
    }
}
