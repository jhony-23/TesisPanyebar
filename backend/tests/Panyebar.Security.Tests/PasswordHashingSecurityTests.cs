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

        var actor = new UsuarioAdministrativo
        {
            NombreUsuario = "actor.admin",
            PasswordHash = _passwordHashService.Hash("ActorPassword123!"),
            Estado = EstadoRegistro.Activo
        };

        dbContext.UsuariosAdministrativos.Add(actor);
        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(
            dbContext,
            new PasswordHasherAdapter());

        var result = await service.CreateUsuarioAsync(
            "nuevo.admin",
            "Password123!",
            actor.Id,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("nuevo.admin", result.Value!.NombreUsuario);

        var persistedUser =
            await dbContext.UsuariosAdministrativos.SingleAsync(
                u => u.NombreUsuario == "nuevo.admin");

        Assert.NotEqual(
            "Password123!",
            persistedUser.PasswordHash);

        Assert.True(
            new PasswordHasherAdapter().Verify(
                persistedUser.PasswordHash,
                "Password123!"));

        Assert.Null(
            typeof(UsuarioAdministrativoAuthenticationResult)
                .GetProperty("PasswordHash"));
    }

    [Fact]
    public async Task CreateUsuarioAsync_RejectsDuplicateNombreUsuario()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        var actor = new UsuarioAdministrativo
        {
            NombreUsuario = "actor.duplicate",
            PasswordHash = _passwordHashService.Hash("ActorPassword123!"),
            Estado = EstadoRegistro.Activo
        };

        dbContext.UsuariosAdministrativos.Add(actor);
        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(
            dbContext,
            new PasswordHasherAdapter());

        var first = await service.CreateUsuarioAsync(
            "duplicado",
            "Password123!",
            actor.Id,
            CancellationToken.None);

        var second = await service.CreateUsuarioAsync(
            "duplicado",
            "OtraPassword123!",
            actor.Id,
            CancellationToken.None);

        Assert.True(first.Succeeded);

        Assert.Equal(
            AdministrativeAccessError.Duplicate,
            second.Error);

        Assert.Equal(
            1,
            await dbContext.UsuariosAdministrativos.CountAsync(
                u => u.NombreUsuario == "duplicado"));
    }

    [Fact]
    public async Task SetRolesForUsuarioAsync_SynchronizesFinalRoleList_WithoutDuplicating()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        dbContext.Roles.AddRange(
            new Rol
            {
                Id = 1,
                Nombre = "Admin",
                Estado = EstadoRegistro.Activo
            },
            new Rol
            {
                Id = 2,
                Nombre = "Auditor",
                Estado = EstadoRegistro.Activo
            },
            new Rol
            {
                Id = 3,
                Nombre = "Operador",
                Estado = EstadoRegistro.Activo
            });

        var user = new UsuarioAdministrativo
        {
            Id = 10,
            NombreUsuario = "admin.sync",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var securityActor = new UsuarioAdministrativo
        {
            Id = 20,
            NombreUsuario = "security.actor",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var securityRole = new Rol
        {
            Id = 100,
            Nombre = "Security Administrator",
            Estado = EstadoRegistro.Activo
        };

        var criticalPermissions = CreateCriticalPermissions(100);

        dbContext.UsuariosAdministrativos.AddRange(
            user,
            securityActor);

        dbContext.Roles.Add(securityRole);

        dbContext.Permisos.AddRange(criticalPermissions);

        dbContext.UsuarioRoles.AddRange(
            new UsuarioRol
            {
                UsuarioAdministrativoId = user.Id,
                RolId = 1
            },
            new UsuarioRol
            {
                UsuarioAdministrativoId = user.Id,
                RolId = 3
            },
            new UsuarioRol
            {
                UsuarioAdministrativoId = securityActor.Id,
                RolId = securityRole.Id
            });

        foreach (var permission in criticalPermissions)
        {
            dbContext.RolPermisos.Add(
                new RolPermiso
                {
                    RolId = securityRole.Id,
                    PermisoId = permission.Id
                });
        }

        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(
            dbContext,
            new PasswordHasherAdapter());

        var assigned = await service.SetRolesForUsuarioAsync(
            user.Id,
            new[] { 2, 2, 1 },
            securityActor.Id,
            CancellationToken.None);

        Assert.True(assigned.Succeeded);

        var userRoles = await dbContext.UsuarioRoles
            .Where(x =>
                x.UsuarioAdministrativoId == user.Id)
            .OrderBy(x => x.RolId)
            .ToListAsync();

        Assert.Equal(
            new[] { 1, 2 },
            userRoles.Select(x => x.RolId));

        var cleared = await service.SetRolesForUsuarioAsync(
            user.Id,
            Array.Empty<int>(),
            securityActor.Id,
            CancellationToken.None);

        Assert.True(cleared.Succeeded);

        Assert.Empty(
            await dbContext.UsuarioRoles
                .Where(x =>
                    x.UsuarioAdministrativoId == user.Id)
                .ToListAsync());

        Assert.Equal(
            2,
            await dbContext.UsuariosAdministrativos.CountAsync());

        Assert.Equal(
            4,
            await dbContext.Roles.CountAsync());
    }

    [Fact]
    public async Task SetUsuarioEstadoAsync_UpdatesEstado_AndPersistsInactive()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        var target = new UsuarioAdministrativo
        {
            Id = 11,
            NombreUsuario = "estado.user",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var securityActor = new UsuarioAdministrativo
        {
            Id = 12,
            NombreUsuario = "estado.actor",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var securityRole = new Rol
        {
            Id = 110,
            Nombre = "Estado Security Admin",
            Estado = EstadoRegistro.Activo
        };

        var criticalPermissions = CreateCriticalPermissions(110);

        dbContext.UsuariosAdministrativos.AddRange(
            target,
            securityActor);

        dbContext.Roles.Add(securityRole);
        dbContext.Permisos.AddRange(criticalPermissions);

        dbContext.UsuarioRoles.Add(
            new UsuarioRol
            {
                UsuarioAdministrativoId = securityActor.Id,
                RolId = securityRole.Id
            });

        foreach (var permission in criticalPermissions)
        {
            dbContext.RolPermisos.Add(
                new RolPermiso
                {
                    RolId = securityRole.Id,
                    PermisoId = permission.Id
                });
        }

        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(
            dbContext,
            new PasswordHasherAdapter());

        var result = await service.SetUsuarioEstadoAsync(
            target.Id,
            EstadoRegistro.Inactivo,
            securityActor.Id,
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var user =
            await dbContext.UsuariosAdministrativos.SingleAsync(
                x => x.Id == target.Id);

        Assert.Equal(
            EstadoRegistro.Inactivo,
            user.Estado);
    }

    [Fact]
    public async Task SetPermisosForRolAsync_SynchronizesFinalPermissionList_WithoutDuplicating()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        var role = new Rol
        {
            Id = 4,
            Nombre = "Supervisor",
            Estado = EstadoRegistro.Activo
        };

        var permiso1 = new Permiso
        {
            Id = 1,
            Codigo = "SEGURIDAD.P1",
            Nombre = "Permiso 1",
            Estado = EstadoRegistro.Activo
        };

        var permiso2 = new Permiso
        {
            Id = 2,
            Codigo = "SEGURIDAD.P2",
            Nombre = "Permiso 2",
            Estado = EstadoRegistro.Activo
        };

        var securityActor = new UsuarioAdministrativo
        {
            Id = 30,
            NombreUsuario = "permission.actor",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var securityRole = new Rol
        {
            Id = 120,
            Nombre = "Permission Security Admin",
            Estado = EstadoRegistro.Activo
        };

        var criticalPermissions = CreateCriticalPermissions(120);

        dbContext.Roles.AddRange(role, securityRole);

        dbContext.Permisos.AddRange(
            permiso1,
            permiso2);

        dbContext.Permisos.AddRange(criticalPermissions);

        dbContext.UsuariosAdministrativos.Add(securityActor);

        dbContext.RolPermisos.Add(
            new RolPermiso
            {
                RolId = role.Id,
                PermisoId = permiso1.Id
            });

        dbContext.UsuarioRoles.Add(
            new UsuarioRol
            {
                UsuarioAdministrativoId = securityActor.Id,
                RolId = securityRole.Id
            });

        foreach (var permission in criticalPermissions)
        {
            dbContext.RolPermisos.Add(
                new RolPermiso
                {
                    RolId = securityRole.Id,
                    PermisoId = permission.Id
                });
        }

        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(
            dbContext,
            new PasswordHasherAdapter());

        var assigned = await service.SetPermisosForRolAsync(
            role.Id,
            new[] { 2, 2, 1 },
            securityActor.Id,
            CancellationToken.None);

        Assert.True(assigned.Succeeded);

        var permissions = await dbContext.RolPermisos
            .Where(x => x.RolId == role.Id)
            .OrderBy(x => x.PermisoId)
            .ToListAsync();

        Assert.Equal(
            new[] { 1, 2 },
            permissions.Select(x => x.PermisoId));

        var cleared = await service.SetPermisosForRolAsync(
            role.Id,
            Array.Empty<int>(),
            securityActor.Id,
            CancellationToken.None);

        Assert.True(cleared.Succeeded);

        Assert.Empty(
            await dbContext.RolPermisos
                .Where(x => x.RolId == role.Id)
                .ToListAsync());

        Assert.Equal(
            2,
            await dbContext.Roles.CountAsync());

        Assert.Equal(
            5,
            await dbContext.Permisos.CountAsync());
    }

    [Fact]
    public async Task SetRolEstadoAsync_UpdatesEstado_AndPersistsInactive()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PanyebarDbContext(options);

        var targetRole = new Rol
        {
            Id = 20,
            Nombre = "EstadoRol",
            Estado = EstadoRegistro.Activo
        };

        var securityActor = new UsuarioAdministrativo
        {
            Id = 40,
            NombreUsuario = "role.actor",
            PasswordHash = _passwordHashService.Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var securityRole = new Rol
        {
            Id = 130,
            Nombre = "Role Security Admin",
            Estado = EstadoRegistro.Activo
        };

        var criticalPermissions = CreateCriticalPermissions(130);

        dbContext.Roles.AddRange(
            targetRole,
            securityRole);

        dbContext.UsuariosAdministrativos.Add(securityActor);
        dbContext.Permisos.AddRange(criticalPermissions);

        dbContext.UsuarioRoles.Add(
            new UsuarioRol
            {
                UsuarioAdministrativoId = securityActor.Id,
                RolId = securityRole.Id
            });

        foreach (var permission in criticalPermissions)
        {
            dbContext.RolPermisos.Add(
                new RolPermiso
                {
                    RolId = securityRole.Id,
                    PermisoId = permission.Id
                });
        }

        await dbContext.SaveChangesAsync();

        var service = new AdministrativeAccessService(
            dbContext,
            new PasswordHasherAdapter());

        var result = await service.SetRolEstadoAsync(
            targetRole.Id,
            EstadoRegistro.Inactivo,
            securityActor.Id,
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var role = await dbContext.Roles
            .SingleAsync(x => x.Id == targetRole.Id);

        Assert.Equal(
            EstadoRegistro.Inactivo,
            role.Estado);
    }

    private static Permiso[] CreateCriticalPermissions(int startId)
    {
        return new[]
        {
            new Permiso
            {
                Id = startId,
                Codigo = AdministrativePermissionCodes.UsuariosGestionar,
                Nombre = "Usuarios gestionar",
                Estado = EstadoRegistro.Activo
            },
            new Permiso
            {
                Id = startId + 1,
                Codigo = AdministrativePermissionCodes.RolesGestionar,
                Nombre = "Roles gestionar",
                Estado = EstadoRegistro.Activo
            },
            new Permiso
            {
                Id = startId + 2,
                Codigo = AdministrativePermissionCodes.PermisosAsignar,
                Nombre = "Permisos asignar",
                Estado = EstadoRegistro.Activo
            }
        };
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
