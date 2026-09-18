using System.Net;
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
using Panyebar.Application.AdministracionComite;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class AdministracionComiteHttpSecurityTests : IAsyncLifetime
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
            IAdministracionComiteService,
            AdministracionComiteService>();

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
                typeof(AdministracionComiteController).Assembly);

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
                AdministrativePermissionCodes.AdministracionVer,
                AdministrativePermissionCodes.AdministracionGestionar
            };

            for (var id = 1; id <= codes.Length; id++)
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

    [Theory]
    [InlineData("/api/administracion-comite")]
    [InlineData("/api/administracion-comite/cargos")]
    public async Task Reads_require_view_permission(string path)
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Send(HttpMethod.Get, path)).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await Send(HttpMethod.Get, path, "1")).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await Send(HttpMethod.Get, path, "2")).StatusCode);
    }

    [Fact]
    public async Task Create_requires_manage_permission()
    {
        var body =
            """{"nombre":"Comité 2026","fechaInicio":"2026-01-01"}""";

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Send(
                HttpMethod.Post,
                "/api/administracion-comite",
                null,
                body)).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await Send(
                HttpMethod.Post,
                "/api/administracion-comite",
                "1",
                body)).StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            (await Send(
                HttpMethod.Post,
                "/api/administracion-comite",
                "2",
                body)).StatusCode);
    }

    [Theory]
    [InlineData("""{"nombre":"Comité","fechaInicio":"2026-13-01"}""")]
    [InlineData("""{"nombre":"Comité","fechaInicio":"2026-02-30"}""")]
    [InlineData("""{"nombre":"","fechaInicio":"2026-01-01"}""")]
    public async Task Create_rejects_invalid_input(string body)
    {
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await Send(
                HttpMethod.Post,
                "/api/administracion-comite",
                "2",
                body)).StatusCode);
    }

    private async Task<HttpResponseMessage> Send(
        HttpMethod method,
        string path,
        string? user = null,
        string? json = null)
    {
        using var request =
            new HttpRequestMessage(method, path);

        if (user is not null)
        {
            request.Headers.Add("X-Test-User", user);
        }

        if (json is not null)
        {
            request.Content =
                new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");
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
