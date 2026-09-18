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
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class AdministrativeAuditHttpSecurityTests : IAsyncLifetime
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

        builder.Services.AddScoped<
            IAdministrativeAccessService,
            AdministrativeAccessService>();

        builder.Services.AddScoped<
            IPasswordHashService,
            PasswordHasherAdapter>();

        builder.Services.AddScoped<
            IUsuarioPermissionRepository,
            UsuarioPermissionRepository>();


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
            .AddApplicationPart(
                typeof(AdministrativeAccessController).Assembly);

        _app = builder.Build();

        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();

        using (var scope = _app.Services.CreateScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<PanyebarDbContext>();

            var codes = new[]
            {
                AdministrativePermissionCodes.SeguridadAuditoriaVer,
                "PERSONAS.VER"
            };

            for (var id = 1; id <= codes.Length; id++)
            {
                db.UsuariosAdministrativos.Add(
                    new UsuarioAdministrativo
                    {
                        Id = id,
                        NombreUsuario = $"audit-user-{id}"
                    });

                db.Roles.Add(
                    new Rol
                    {
                        Id = id,
                        Nombre = $"audit-role-{id}"
                    });

                db.UsuarioRoles.Add(
                    new UsuarioRol
                    {
                        Id = id,
                        UsuarioAdministrativoId = id,
                        RolId = id
                    });

                db.Permisos.Add(
                    new Permiso
                    {
                        Id = id,
                        Codigo = codes[id - 1],
                        Nombre = codes[id - 1]
                    });

                db.RolPermisos.Add(
                    new RolPermiso
                    {
                        Id = id,
                        RolId = id,
                        PermisoId = id
                    });
            }

            db.Auditorias.AddRange(
                new Auditoria
                {
                    Id = 1,
                    UsuarioAdministrativoId = 1,
                    Accion = "PAGO.REGISTRAR",
                    Entidad = "Pago",
                    EntidadId = 10,
                    Fecha = new DateTime(
                        2026, 9, 17, 12, 0, 0,
                        DateTimeKind.Utc),
                    ValorNuevo = """{"monto":50}"""
                },
                new Auditoria
                {
                    Id = 2,
                    UsuarioAdministrativoId = 1,
                    Accion = "PERSONA.CREAR",
                    Entidad = "Persona",
                    EntidadId = 20,
                    Fecha = new DateTime(
                        2026, 9, 18, 12, 0, 0,
                        DateTimeKind.Utc)
                });

            await db.SaveChangesAsync();
        }

        await _app.StartAsync();

        var address =
            _app.Services
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
    public async Task Audit_requires_authentication()
    {
        var response = await Send(
            "/api/admin/auditoria");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Audit_forbids_wrong_permission()
    {
        var response = await Send(
            "/api/admin/auditoria",
            "2");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Audit_allows_specific_permission()
    {
        var response = await Send(
            "/api/admin/auditoria",
            "1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Theory]
    [InlineData("usuarioId=0")]
    [InlineData("limite=0")]
    [InlineData("limite=501")]
    public async Task Audit_rejects_invalid_numeric_filters(
        string query)
    {
        var response = await Send(
            "/api/admin/auditoria?" + query,
            "1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Audit_rejects_invalid_date_range()
    {
        var response = await Send(
            "/api/admin/auditoria" +
            "?fechaDesde=2026-09-18T00:00:00Z" +
            "&fechaHasta=2026-09-17T00:00:00Z",
            "1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Audit_returns_filtered_contract_without_writing_audit()
    {
        int before;

        using (var scope = _app.Services.CreateScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<PanyebarDbContext>();

            before = await db.Auditorias.CountAsync();
        }

        var response = await Send(
            "/api/admin/auditoria" +
            "?accion=PAGO" +
            "&entidad=Pago" +
            "&usuarioId=1" +
            "&limite=20",
            "1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var rows =
            await response.Content
                .ReadFromJsonAsync<
                    List<AuditoriaAdministrativaDto>>();

        var row = Assert.Single(rows!);

        Assert.Equal(1, row.Id);
        Assert.Equal(
            "audit-user-1",
            row.NombreUsuario);
        Assert.Equal(
            "PAGO.REGISTRAR",
            row.Accion);
        Assert.Equal(
            "Pago",
            row.Entidad);
        Assert.Equal(10, row.EntidadId);
        Assert.NotNull(row.ValorNuevo);

        using var verifyScope =
            _app.Services.CreateScope();

        var verifyDb =
            verifyScope.ServiceProvider
                .GetRequiredService<PanyebarDbContext>();

        Assert.Equal(
            before,
            await verifyDb.Auditorias.CountAsync());
    }

    private async Task<HttpResponseMessage> Send(
        string path,
        string? user = null)
    {
        using var request =
            new HttpRequestMessage(
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

    private sealed class TestIdentityHandler :
        AuthenticationHandler<AuthenticationSchemeOptions>
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

            var identity =
                new ClaimsIdentity(
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
