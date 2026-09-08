using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Panyebar.Application.Security;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public class PasswordHashingSecurityTests
{
    private readonly IPasswordHashService _passwordHashService = new PasswordHasherAdapter();

    [Theory]
    [InlineData(1, "PAN-000001")]
    [InlineData(25, "PAN-000025")]
    [InlineData(123, "PAN-000123")]
    public void NisFormatter_FormatsExpectedValue(long value, string expected)
    {
        Assert.Equal(expected, NisFormatter.Format(value));
    }

    [Fact]
    public void SuministroQrTokenGenerator_CreatesDistinctUrlSafeTokens()
    {
        var generator = new SuministroQrTokenGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.Equal(43, first.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", first);
        Assert.NotEmpty(first);
        Assert.NotEqual(first, second);
    }

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
        Assert.Equal("7", token.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("admin", token.Claims.First(x => x.Type == JwtRegisteredClaimNames.UniqueName).Value);
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

    [Fact]
    public async Task CreateUsuarioAsync_CreatesUser_StoresHashedPassword_AndDoesNotExposeHash()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);
        var service = new AdministrativeAccessService(dbContext, new PasswordHasherAdapter());

        var createdUser = await service.CreateUsuarioAsync("nuevo.admin", "Password123!", CancellationToken.None);

        Assert.NotNull(createdUser);
        Assert.Equal("nuevo.admin", createdUser!.NombreUsuario);

        var persistedUser = await dbContext.UsuariosAdministrativos.SingleAsync();
        Assert.NotEqual("Password123!", persistedUser.PasswordHash);
        Assert.True(new PasswordHasherAdapter().Verify(persistedUser.PasswordHash, "Password123!"));
        Assert.Equal("nuevo.admin", createdUser.NombreUsuario);
        Assert.Null(typeof(UsuarioAdministrativoAuthenticationResult).GetProperty("PasswordHash"));
    }

    [Fact]
    public async Task CreateUsuarioAsync_RejectsDuplicateNombreUsuario()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);
        var service = new AdministrativeAccessService(dbContext, new PasswordHasherAdapter());

        var first = await service.CreateUsuarioAsync("duplicado", "Password123!", CancellationToken.None);
        var second = await service.CreateUsuarioAsync("duplicado", "OtraPassword123!", CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal(1, await dbContext.UsuariosAdministrativos.CountAsync());
    }

    [Fact]
    public async Task SetRolesForUsuarioAsync_SynchronizesFinalRoleList_WithoutDuplicating()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        dbContext.Roles.AddRange(
            new Rol { Id = 1, Nombre = "Admin", Estado = EstadoRegistro.Activo },
            new Rol { Id = 2, Nombre = "Auditor", Estado = EstadoRegistro.Activo },
            new Rol { Id = 3, Nombre = "Operador", Estado = EstadoRegistro.Activo });

        var user = new UsuarioAdministrativo { Id = 10, NombreUsuario = "admin.sync", PasswordHash = _passwordHashService.Hash("Password123!"), Estado = EstadoRegistro.Activo };
        dbContext.UsuariosAdministrativos.Add(user);
        dbContext.UsuarioRoles.Add(new UsuarioRol { UsuarioAdministrativoId = user.Id, RolId = 1 });
        dbContext.UsuarioRoles.Add(new UsuarioRol { UsuarioAdministrativoId = user.Id, RolId = 3 });
        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(dbContext, new PasswordHasherAdapter());

        var assigned = await service.SetRolesForUsuarioAsync(user.Id, new[] { 2, 2, 1 }, CancellationToken.None);
        Assert.True(assigned);

        var userRoles = await dbContext.UsuarioRoles.Where(x => x.UsuarioAdministrativoId == user.Id).OrderBy(x => x.RolId).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, userRoles.Select(x => x.RolId));

        var cleared = await service.SetRolesForUsuarioAsync(user.Id, Array.Empty<int>(), CancellationToken.None);
        Assert.True(cleared);
        Assert.Empty(await dbContext.UsuarioRoles.Where(x => x.UsuarioAdministrativoId == user.Id).ToListAsync());

        Assert.Equal(1, await dbContext.UsuariosAdministrativos.CountAsync());
        Assert.Equal(3, await dbContext.Roles.CountAsync());
    }

    [Fact]
    public async Task SetUsuarioEstadoAsync_UpdatesEstado_AndPersistsInactive()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);
        dbContext.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = 11, NombreUsuario = "estado.user", PasswordHash = _passwordHashService.Hash("Password123!"), Estado = EstadoRegistro.Activo });
        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(dbContext, new PasswordHasherAdapter());

        var result = await service.SetUsuarioEstadoAsync(11, EstadoRegistro.Inactivo, CancellationToken.None);

        Assert.True(result);
        var user = await dbContext.UsuariosAdministrativos.SingleAsync(x => x.Id == 11);
        Assert.Equal(EstadoRegistro.Inactivo, user.Estado);
    }

    [Fact]
    public async Task SetPermisosForRolAsync_SynchronizesFinalPermissionList_WithoutDuplicating()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        var role = new Rol { Id = 4, Nombre = "Supervisor", Estado = EstadoRegistro.Activo };
        var permiso1 = new Permiso { Id = 1, Codigo = "SEGURIDAD.P1", Nombre = "Permiso 1", Estado = EstadoRegistro.Activo };
        var permiso2 = new Permiso { Id = 2, Codigo = "SEGURIDAD.P2", Nombre = "Permiso 2", Estado = EstadoRegistro.Activo };
        dbContext.Roles.Add(role);
        dbContext.Permisos.AddRange(permiso1, permiso2);
        dbContext.RolPermisos.Add(new RolPermiso { RolId = role.Id, PermisoId = permiso1.Id });
        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(dbContext, new PasswordHasherAdapter());

        var assigned = await service.SetPermisosForRolAsync(role.Id, new[] { 2, 2, 1 }, CancellationToken.None);
        Assert.True(assigned);

        var permissions = await dbContext.RolPermisos.Where(x => x.RolId == role.Id).OrderBy(x => x.PermisoId).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, permissions.Select(x => x.PermisoId));

        var cleared = await service.SetPermisosForRolAsync(role.Id, Array.Empty<int>(), CancellationToken.None);
        Assert.True(cleared);
        Assert.Empty(await dbContext.RolPermisos.Where(x => x.RolId == role.Id).ToListAsync());

        Assert.Equal(1, await dbContext.Roles.CountAsync());
        Assert.Equal(2, await dbContext.Permisos.CountAsync());
    }

    [Fact]
    public async Task SetRolEstadoAsync_UpdatesEstado_AndPersistsInactive()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);
        dbContext.Roles.Add(new Rol { Id = 20, Nombre = "EstadoRol", Estado = EstadoRegistro.Activo });
        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(dbContext, new PasswordHasherAdapter());

        var result = await service.SetRolEstadoAsync(20, EstadoRegistro.Inactivo, CancellationToken.None);

        Assert.True(result);
        var role = await dbContext.Roles.SingleAsync(x => x.Id == 20);
        Assert.Equal(EstadoRegistro.Inactivo, role.Estado);
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
