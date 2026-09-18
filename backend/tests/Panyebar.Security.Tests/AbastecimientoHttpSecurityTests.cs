using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Panyebar.Api.Controllers;
using Panyebar.Application.Abastecimiento;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class AbastecimientoHttpSecurityTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });

        builder.Logging.ClearProviders();

        builder.WebHost
            .UseKestrel()
            .UseUrls("http://127.0.0.1:0");

        var database = Guid.NewGuid().ToString();

        builder.Services.AddDbContext<PanyebarDbContext>(
            options => options.UseInMemoryDatabase(database));

        builder.Services.AddScoped<IAbastecimientoService, AbastecimientoService>();
        builder.Services.AddScoped<IUsuarioPermissionRepository, UsuarioPermissionRepository>();

        builder.Services.AddSingleton<
            IAuthorizationPolicyProvider,
            PermissionPolicyProvider>();

        builder.Services.AddScoped<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        builder.Services
            .AddAuthentication("Test")
            .AddScheme<
                AuthenticationSchemeOptions,
                TestIdentityHandler>(
                "Test",
                _ => { });

        builder.Services.AddAuthorization();

        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(AbastecimientoController).Assembly);

        _app = builder.Build();

        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<PanyebarDbContext>();

            // Usuario 1: solo lectura.
            // Usuario 2: solo gestión.
            // Usuario 3: ambos permisos.
            // Usuario 4: autenticado, permiso ajeno.
            var codes = new[]
            {
                AdministrativePermissionCodes.AbastecimientoVer,
                AdministrativePermissionCodes.AbastecimientoGestionar,
                AdministrativePermissionCodes.SectoresVer
            };

            for (var id = 1; id <= 4; id++)
            {
                db.UsuariosAdministrativos.Add(
                    new UsuarioAdministrativo
                    {
                        Id = id,
                        NombreUsuario = $"test-{id}"
                    });

                db.Roles.Add(
                    new Rol
                    {
                        Id = id,
                        Nombre = $"role-{id}"
                    });

                db.UsuarioRoles.Add(
                    new UsuarioRol
                    {
                        Id = id,
                        RolId = id,
                        UsuarioAdministrativoId = id
                    });
            }

            for (var id = 1; id <= codes.Length; id++)
            {
                db.Permisos.Add(
                    new Permiso
                    {
                        Id = id,
                        Codigo = codes[id - 1],
                        Nombre = codes[id - 1]
                    });
            }

            // Usuario 1 -> ABASTECIMIENTO.VER
            db.RolPermisos.Add(
                new RolPermiso
                {
                    Id = 1,
                    RolId = 1,
                    PermisoId = 1
                });

            // Usuario 2 -> ABASTECIMIENTO.GESTIONAR
            db.RolPermisos.Add(
                new RolPermiso
                {
                    Id = 2,
                    RolId = 2,
                    PermisoId = 2
                });

            // Usuario 3 -> ambos
            db.RolPermisos.AddRange(
                new RolPermiso
                {
                    Id = 3,
                    RolId = 3,
                    PermisoId = 1
                },
                new RolPermiso
                {
                    Id = 4,
                    RolId = 3,
                    PermisoId = 2
                });

            // Usuario 4 -> permiso ajeno
            db.RolPermisos.Add(
                new RolPermiso
                {
                    Id = 5,
                    RolId = 4,
                    PermisoId = 3
                });

            db.Sectores.Add(
                new Sector
                {
                    Id = 1,
                    Nombre = "Sector QA",
                    Estado = EstadoRegistro.Activo
                });

            await db.SaveChangesAsync();
        }

        await _app.StartAsync();

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();

        _client = new HttpClient
        {
            BaseAddress = new Uri(address)
        };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Read_ChallengeAnonymous()
    {
        var response = await SendGet("/api/abastecimiento");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Read_ForbidsUserWithoutReadPermission()
    {
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendGet("/api/abastecimiento", "2")).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendGet("/api/abastecimiento", "4")).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendGet("/api/abastecimiento", "999")).StatusCode);
    }

    [Fact]
    public async Task Read_AllowsReadPermission()
    {
        var response = await SendGet(
            "/api/abastecimiento",
            "1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var scope = _app.Services.CreateScope();

        Assert.Empty(
            scope.ServiceProvider
                .GetRequiredService<PanyebarDbContext>()
                .Auditorias);
    }

    [Fact]
    public async Task Read_AllowsUserWithBothPermissions()
    {
        Assert.Equal(
            HttpStatusCode.OK,
            (await SendGet("/api/abastecimiento", "3")).StatusCode);
    }

    [Fact]
    public async Task Mutation_ChallengeAnonymous()
    {
        var response = await SendPost(
            "/api/abastecimiento",
            ValidInput());

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Mutation_ForbidsReadOnlyUser()
    {
        var response = await SendPost(
            "/api/abastecimiento",
            ValidInput(),
            "1");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Mutation_AllowsManagementPermission()
    {
        var response = await SendPost(
            "/api/abastecimiento",
            ValidInput(),
            "2");

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var dto = await response.Content
            .ReadFromJsonAsync<ProgramacionAbastecimientoDto>();

        Assert.NotNull(dto);
        Assert.Equal(
            EstadosProgramacionAbastecimiento.Programado,
            dto!.Estado);

        using var scope = _app.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<PanyebarDbContext>();

        Assert.Single(db.ProgramacionesAbastecimiento);

        Assert.Contains(
            db.Auditorias,
            audit => audit.Accion == "ABASTECIMIENTO.CREAR");
    }

    [Fact]
    public async Task Mutation_AllowsUserWithBothPermissions()
    {
        var input = new ProgramacionAbastecimientoInput(
            1,
            new DateOnly(2026, 9, 21),
            new TimeSpan(6, 0, 0),
            new TimeSpan(10, 0, 0),
            null);

        var response = await SendPost(
            "/api/abastecimiento",
            input,
            "3");

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    [Theory]
    [InlineData("/api/abastecimiento?fechaDesde=2026-09-21&fechaHasta=2026-09-20")]
    [InlineData("/api/abastecimiento?sectorId=0")]
    [InlineData("/api/abastecimiento?sectorId=-1")]
    public async Task InvalidFilters_ReturnControlledBadRequest(
        string path)
    {
        var response = await SendGet(path, "1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task PermissionRevocation_IsEffectiveOnNextRequest()
    {
        Assert.Equal(
            HttpStatusCode.OK,
            (await SendGet("/api/abastecimiento", "1")).StatusCode);

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<PanyebarDbContext>();

            var permission = await db.Permisos
                .SingleAsync(
                    p => p.Codigo ==
                        AdministrativePermissionCodes.AbastecimientoVer);

            permission.Estado = EstadoRegistro.Inactivo;

            await db.SaveChangesAsync();
        }

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendGet("/api/abastecimiento", "1")).StatusCode);
    }

    private static ProgramacionAbastecimientoInput ValidInput() =>
        new(
            1,
            new DateOnly(2026, 9, 20),
            new TimeSpan(6, 0, 0),
            new TimeSpan(10, 0, 0),
            "Prueba HTTP");

    private async Task<HttpResponseMessage> SendGet(
        string path,
        string? user = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            path);

        if (user is not null)
        {
            request.Headers.Add(
                "X-Test-User",
                user);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendPost<T>(
        string path,
        T content,
        string? user = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            path)
        {
            Content = JsonContent.Create(content)
        };

        if (user is not null)
        {
            request.Headers.Add(
                "X-Test-User",
                user);
        }

        return await _client.SendAsync(request);
    }

    private sealed class TestIdentityHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestIdentityHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult>
            HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(
                "X-Test-User",
                out var user))
            {
                return Task.FromResult(
                    AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.ToString())
                },
                Scheme.Name);

            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(identity),
                        Scheme.Name)));
        }
    }
}
