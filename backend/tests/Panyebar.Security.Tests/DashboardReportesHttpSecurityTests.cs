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
using Panyebar.Application.DashboardReportes;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

// Identidad de prueba, con policies, repositorio dinámico y servicios reales.
public sealed class DashboardReportesHttpSecurityTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<PanyebarDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddScoped<IDashboardReportesService, DashboardReportesService>();
        builder.Services.AddScoped<IUsuarioPermissionRepository, UsuarioPermissionRepository>();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestIdentityHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(ReportesController).Assembly);
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PanyebarDbContext>();
            var codes = new[] { AdministrativePermissionCodes.DashboardVer, AdministrativePermissionCodes.ReportesVer,
                AdministrativePermissionCodes.FinanzasGestionar };
            for (var id = 1; id <= codes.Length; id++)
            {
                db.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = id, NombreUsuario = $"test-{id}" });
                db.Roles.Add(new Rol { Id = id, Nombre = $"role-{id}" });
                db.UsuarioRoles.Add(new UsuarioRol { Id = id, RolId = id, UsuarioAdministrativoId = id });
                db.Permisos.Add(new Permiso { Id = id, Codigo = codes[id - 1], Nombre = codes[id - 1] });
                db.RolPermisos.Add(new RolPermiso { Id = id, RolId = id, PermisoId = id });
            }
            await db.SaveChangesAsync();
        }
        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/reportes/pagos", "2", "1")]
    [InlineData("/api/reportes/obligaciones-pendientes", "2", "1")]
    [InlineData("/api/reportes/jornadas", "2", "1")]
    [InlineData("/api/dashboard/resumen?anio=2026&mes=9", "1", "2")]
    public async Task Reads_ChallengeAnonymousForbidWrongPermissionAndAllowCorrespondingPermission(
        string path, string allowed, string wrong)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(path, wrong)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(path, "3")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(path, "999")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(path, allowed)).StatusCode);
        using var scope = _app.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<PanyebarDbContext>().Auditorias);
    }

    [Theory]
    [InlineData("/api/reportes/pagos?fechaDesde=2026-09-17&fechaHasta=2026-09-16")]
    [InlineData("/api/reportes/jornadas?fechaDesde=2026-09-17&fechaHasta=2026-09-16")]
    [InlineData("/api/reportes/pagos?fechaDesde=2026-13-01")]
    [InlineData("/api/reportes/jornadas?fechaHasta=2026-02-30")]
    [InlineData("/api/reportes/pagos?fechaDesde=2026-09-16T00:00:00Z")]
    public async Task InvalidReportFilters_ReturnControlledBadRequest(string path) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(path, "2")).StatusCode);

    [Theory]
    [InlineData("anio=2026&mes=0")]
    [InlineData("anio=2026&mes=13")]
    [InlineData("anio=0&mes=1")]
    [InlineData("anio=10000&mes=1")]
    [InlineData("anio=9999&mes=12")]
    [InlineData("anio=texto&mes=1")]
    [InlineData("anio=2026")]
    [InlineData("mes=1")]
    public async Task InvalidDashboardMonth_ReturnsBadRequest(string query) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await Send("/api/dashboard/resumen?" + query, "1")).StatusCode);

    [Fact]
    public async Task Dashboard_ReturnsMonthlyContract()
    {
        var response = await Send("/api/dashboard/resumen?anio=2026&mes=9", "1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DashboardResumenDto(2026, 9, 0m, 0m, 0m, 0, 0, 0m),
            await response.Content.ReadFromJsonAsync<DashboardResumenDto>());
    }

    [Fact]
    public async Task PermissionRevocation_IsEffectiveOnNextRequest()
    {
        Assert.Equal(HttpStatusCode.OK, (await Send("/api/reportes/pagos", "2")).StatusCode);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PanyebarDbContext>();
            (await db.Permisos.SingleAsync(p => p.Codigo == AdministrativePermissionCodes.ReportesVer)).Estado = EstadoRegistro.Inactivo;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await Send("/api/reportes/pagos", "2")).StatusCode);
    }

    private async Task<HttpResponseMessage> Send(string path, string? user = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (user is not null) request.Headers.Add("X-Test-User", user);
        return await _client.SendAsync(request);
    }

    private sealed class TestIdentityHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestIdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var user))
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
