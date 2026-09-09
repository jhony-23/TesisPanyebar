using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Api.Controllers;
using Panyebar.Application.Cuotas;
using Panyebar.Application.Obligaciones;
using Panyebar.Domain.Enums;

namespace Panyebar.Security.Tests;

public sealed class FinanceControllersSecurityTests
{
    [Theory]
    [InlineData(typeof(CuotasController), nameof(CuotasController.GetAll), "Permission:CUOTAS.VER")]
    [InlineData(typeof(CuotasController), nameof(CuotasController.GetById), "Permission:CUOTAS.VER")]
    [InlineData(typeof(CuotasController), nameof(CuotasController.Create), "Permission:CUOTAS.GESTIONAR")]
    [InlineData(typeof(CuotasController), nameof(CuotasController.Update), "Permission:CUOTAS.GESTIONAR")]
    [InlineData(typeof(CuotasController), nameof(CuotasController.SetEstado), "Permission:CUOTAS.GESTIONAR")]
    [InlineData(typeof(ObligacionesController), nameof(ObligacionesController.GetAll), "Permission:OBLIGACIONES.VER")]
    [InlineData(typeof(ObligacionesController), nameof(ObligacionesController.GetById), "Permission:OBLIGACIONES.VER")]
    [InlineData(typeof(ObligacionesController), nameof(ObligacionesController.GenerateFromCuota), "Permission:OBLIGACIONES.GESTIONAR")]
    [InlineData(typeof(ObligacionesController), nameof(ObligacionesController.Annul), "Permission:OBLIGACIONES.GESTIONAR")]
    public void Endpoints_RequireExpectedDynamicPermission(Type controllerType, string action, string policy)
    {
        var method = controllerType.GetMethod(action, BindingFlags.Instance | BindingFlags.Public);

        var authorize = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(policy, authorize.Policy);
    }

    [Fact]
    public async Task GenerateFromCuota_UsesAuthenticatedClaimAsAdministrativeAuthor()
    {
        var service = new CapturingObligacionService();
        var controller = new ObligacionesController(service)
        {
            ControllerContext = AuthenticatedContext("42")
        };

        var result = await controller.GenerateFromCuota(
            new GenerarObligacionCuotaInput(1, 2, "2026", null),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(42, service.CapturedUserId);
    }

    [Fact]
    public async Task CreateCuota_UsesAuthenticatedClaimAsAdministrativeAuthor()
    {
        var service = new CapturingCuotaService();
        var controller = new CuotasController(service)
        {
            ControllerContext = AuthenticatedContext("27")
        };

        var result = await controller.Create(
            new CuotaInput("Cuota", null, 30m, PeriodicidadCuota.Anual, new DateTime(2026, 1, 1), null),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(27, service.CapturedUserId);
    }

    [Fact]
    public async Task Annul_RejectsMissingIdentityWithoutCallingService()
    {
        var service = new CapturingObligacionService();
        var controller = new ObligacionesController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Annul(1, new AnularObligacionInput("Motivo"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.CapturedUserId);
    }

    private static ControllerContext AuthenticatedContext(string userId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            "Test");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private sealed class CapturingObligacionService : IObligacionService
    {
        public int? CapturedUserId { get; private set; }

        public Task<IReadOnlyList<ObligacionDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ObligacionDto>>(Array.Empty<ObligacionDto>());

        public Task<ObligacionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ObligacionDto?>(null);

        public Task<ObligacionOperationResult<ObligacionDto>> GenerateFromCuotaAsync(
            GenerarObligacionCuotaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Task.FromResult(ObligacionOperationResult<ObligacionDto>.Success(Result()));
        }

        public Task<ObligacionOperationResult<ObligacionDto>> AnnulAsync(
            int id,
            AnularObligacionInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Task.FromResult(ObligacionOperationResult<ObligacionDto>.Success(Result()));
        }

        private static ObligacionDto Result() => new(
            1,
            null,
            2,
            1,
            OrigenObligacion.CuotaOrdinaria,
            "Cuota",
            30m,
            "2026",
            DateTime.UtcNow,
            null,
            EstadoObligacion.Pendiente,
            false);
    }

    private sealed class CapturingCuotaService : ICuotaService
    {
        public int? CapturedUserId { get; private set; }

        public Task<IReadOnlyList<CuotaDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CuotaDto>>(Array.Empty<CuotaDto>());

        public Task<CuotaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CuotaDto?>(null);

        public Task<CuotaOperationResult<CuotaDto>> CreateAsync(
            CuotaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Task.FromResult(CuotaOperationResult<CuotaDto>.Success(Result()));
        }

        public Task<CuotaOperationResult<CuotaDto>> UpdateAsync(
            int id,
            CuotaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Task.FromResult(CuotaOperationResult<CuotaDto>.Success(Result()));
        }

        public Task<CuotaOperationResult<CuotaDto>> SetEstadoAsync(
            int id,
            EstadoRegistro estado,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Task.FromResult(CuotaOperationResult<CuotaDto>.Success(Result()));
        }

        private static CuotaDto Result() => new(
            1,
            "Cuota",
            null,
            30m,
            PeriodicidadCuota.Anual,
            new DateTime(2026, 1, 1),
            null,
            EstadoRegistro.Activo);
    }
}
