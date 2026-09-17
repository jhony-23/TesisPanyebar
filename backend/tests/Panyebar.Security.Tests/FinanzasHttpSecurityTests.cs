using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Panyebar.Api.Controllers;
using Panyebar.Application.Finanzas;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

// Host HTTP local de tests. El esquema de identidad solo existe en este ensamblado;
// autorización, policies, repositorio de permisos, controllers y servicio son los reales.
public sealed class FinanzasHttpSecurityTests : IAsyncLifetime
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
        builder.Services.AddScoped<IFinanzaService, FinanzaService>();
        builder.Services.AddScoped<IUsuarioPermissionRepository, UsuarioPermissionRepository>();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestIdentityHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(EgresosController).Assembly);
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PanyebarDbContext>();
            var codes = new[] { AdministrativePermissionCodes.FinanzasVer, AdministrativePermissionCodes.FinanzasGestionar,
                AdministrativePermissionCodes.PagosGestionar };
            for (var id = 1; id <= 3; id++)
            {
                db.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = id, NombreUsuario = $"test-{id}" });
                db.Roles.Add(new Rol { Id = id, Nombre = $"test-role-{id}" });
                db.UsuarioRoles.Add(new UsuarioRol { Id = id, RolId = id, UsuarioAdministrativoId = id });
                db.Permisos.Add(new Permiso { Id = id, Codigo = codes[id - 1], Nombre = codes[id - 1] });
                db.RolPermisos.Add(new RolPermiso { Id = id, RolId = id, PermisoId = id });
            }
            db.Egresos.Add(new Egreso { Id = 1, Concepto = "Original", Monto = 10m,
                Fecha = new DateTime(2026, 9, 16), UsuarioAdministrativoId = 1 });
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
    [InlineData("GET", "/api/egresos")]
    [InlineData("GET", "/api/egresos/1")]
    [InlineData("GET", "/api/finanzas/ingresos")]
    [InlineData("GET", "/api/finanzas/movimientos")]
    [InlineData("GET", "/api/finanzas/resumen")]
    [InlineData("POST", "/api/egresos")]
    [InlineData("PUT", "/api/egresos/1")]
    [InlineData("POST", "/api/egresos/1/anulacion")]
    public async Task Endpoints_ChallengeAnonymousAndForbidPaymentsPermission(string method, string path)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(method, path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(method, path, "3")).StatusCode);
    }

    [Theory]
    [InlineData("/api/egresos")]
    [InlineData("/api/egresos/1")]
    [InlineData("/api/finanzas/ingresos")]
    [InlineData("/api/finanzas/movimientos")]
    [InlineData("/api/finanzas/resumen")]
    public async Task Reads_RequireViewPermission(string path)
    {
        Assert.Equal(HttpStatusCode.OK, (await Send("GET", path, "1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send("GET", path, "2")).StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/egresos")]
    [InlineData("PUT", "/api/egresos/1")]
    [InlineData("POST", "/api/egresos/1/anulacion")]
    public async Task Mutations_ForbidViewOnlyPermission(string method, string path)
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(method, path, "1")).StatusCode);
    }

    [Theory]
    [InlineData("NameIdentifier")]
    [InlineData("sub")]
    public async Task Register_UsesTrustedClaimAndIgnoresClientAuthorAndState(string claimType)
    {
        var response = await Send("POST", "/api/egresos", "2", new
        {
            concepto = "Nuevo", monto = 12.50m, fecha = "2026-09-16",
            usuarioAdministrativoId = 999, estado = 2, id = 900
        }, claimType);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<EgresoDto>();
        Assert.Equal(2, dto!.UsuarioAdministrativoId);
        Assert.Equal(Panyebar.Domain.Enums.EstadoEgreso.Registrado, dto.Estado);
        Assert.NotEqual(900, dto.Id);
        Assert.Equal(new DateOnly(2026, 9, 16), dto.Fecha);
        using var scope = _app.Services.CreateScope();
        var audit = Assert.Single(scope.ServiceProvider.GetRequiredService<PanyebarDbContext>().Auditorias);
        Assert.Equal(2, audit.UsuarioAdministrativoId);
    }

    [Fact]
    public async Task UpdateAndAnnul_MapConflictAndPreserveOriginalAuthor()
    {
        var update = await Send("PUT", "/api/egresos/1", "2");
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(1, (await update.Content.ReadFromJsonAsync<EgresoDto>())!.UsuarioAdministrativoId);
        Assert.Equal(HttpStatusCode.OK, (await Send("POST", "/api/egresos/1/anulacion", "2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send("POST", "/api/egresos/1/anulacion", "2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send("PUT", "/api/egresos/1", "2")).StatusCode);
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PanyebarDbContext>();
        Assert.Single(db.Egresos);
        Assert.Equal(2, db.Auditorias.Count());
    }

    [Theory]
    [InlineData("/api/egresos?fechaDesde=2026-09-17&fechaHasta=2026-09-16")]
    [InlineData("/api/finanzas/ingresos?fechaDesde=2026-09-17&fechaHasta=2026-09-16")]
    [InlineData("/api/finanzas/movimientos?tipo=99")]
    [InlineData("/api/finanzas/resumen?fechaDesde=2026-09-17&fechaHasta=2026-09-16")]
    [InlineData("/api/egresos?estado=99")]
    [InlineData("/api/egresos?fechaDesde=2026-13-01")]
    public async Task InvalidFilters_ReturnBadRequest(string path)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Send("GET", path, "1")).StatusCode);
    }

    [Theory]
    [InlineData("2026-09-16T00:00:00Z")]
    [InlineData("2026-09-16T23:00:00-06:00")]
    [InlineData("2026-02-30")]
    [InlineData("0001-01-01")]
    public async Task ExpenseDate_RejectsTimestampsAndInvalidCivilDates(string date)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Send("POST", "/api/egresos", "2",
            new { concepto = "Gasto", monto = 1m, fecha = date })).StatusCode);
    }

    [Fact]
    public async Task MissingExpenseAndInvalidAmount_ReturnExpectedStatus()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Send("GET", "/api/egresos/999", "1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send("PUT", "/api/egresos/999", "2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send("POST", "/api/egresos/999/anulacion", "2")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send("POST", "/api/egresos", "2",
            new { concepto = "Gasto", monto = -1m, fecha = "2026-09-16" })).StatusCode);
    }

    [Fact]
    public async Task NoPhysicalDeleteOrUnauthenticatedIdentityBypass()
    {
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Send("DELETE", "/api/egresos/1", "2")).StatusCode);
        Assert.DoesNotContain(typeof(EgresosController).GetMethods(), method => method.GetCustomAttribute<HttpDeleteAttribute>() is not null);
        var controller = new EgresosController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "2") }))
            } }
        };
        Assert.IsType<UnauthorizedResult>(await controller.Register(new("Gasto", 1m, new(2026, 9, 16)), default));
    }

    private async Task<HttpResponseMessage> Send(string method, string path, string? user = null, object? body = null, string claimType = "NameIdentifier")
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (user is not null) request.Headers.Add("X-Test-User", user);
        request.Headers.Add("X-Test-Claim", claimType);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(body ?? new { concepto = "Corregido", monto = 15m, fecha = "2026-09-16" });
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
            var claim = Request.Headers["X-Test-Claim"] == "sub" ? "sub" : ClaimTypes.NameIdentifier;
            var identity = new ClaimsIdentity(new[] { new Claim(claim, user.ToString()) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
