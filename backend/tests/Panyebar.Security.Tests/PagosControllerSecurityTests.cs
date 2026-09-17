using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Api.Controllers;
using Panyebar.Application.Pagos;
using Panyebar.Domain.Enums;

namespace Panyebar.Security.Tests;

public sealed class PagosControllerSecurityTests
{
    [Theory]
    [InlineData(nameof(PagosController.GetAll), "Permission:PAGOS.VER")]
    [InlineData(nameof(PagosController.GetById), "Permission:PAGOS.VER")]
    [InlineData(nameof(PagosController.GetComprobante), "Permission:PAGOS.VER")]
    [InlineData(nameof(PagosController.Register), "Permission:PAGOS.GESTIONAR")]
    public void Endpoints_RequireExpectedDynamicPermission(
        string action,
        string policy)
    {
        var method = typeof(PagosController)
            .GetMethod(
                action,
                BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        var authorize = Assert.Single(
            method!.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(policy, authorize.Policy);
    }

    [Fact]
    public async Task Register_UsesAuthenticatedClaimAsAdministrativeAuthor()
    {
        var service = new CapturingPagoService
        {
            RegisterResult =
                PagoOperationResult<PagoDetalleDto>.Success(
                    Detail())
        };

        var controller = Controller(service, "42");

        var result = await controller.Register(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { 1 }),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(nameof(PagosController.GetById), created.ActionName);
        Assert.Equal(42, service.CapturedUserId);
    }

    [Fact]
    public async Task Register_RejectsMissingIdentityWithoutCallingService()
    {
        var service = new CapturingPagoService();

        var controller = new PagosController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Register(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { 1 }),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.CapturedUserId);
        Assert.Equal(0, service.RegisterCalls);
    }

    [Theory]
    [InlineData(PagoOperationError.Invalid, typeof(BadRequestObjectResult))]
    [InlineData(PagoOperationError.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(PagoOperationError.Conflict, typeof(ConflictObjectResult))]
    public async Task Register_MapsOperationErrorsToExpectedHttpStatus(
        PagoOperationError error,
        Type expectedResultType)
    {
        var service = new CapturingPagoService
        {
            RegisterResult =
                PagoOperationResult<PagoDetalleDto>.Failure(error)
        };

        var controller = Controller(service, "8");

        var result = await controller.Register(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { 1 }),
            CancellationToken.None);

        Assert.IsType(expectedResultType, result);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenPaymentDoesNotExist()
    {
        var service = new CapturingPagoService();

        var controller = new PagosController(service);

        var result = await controller.GetById(
            999,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetComprobante_ReturnsReceiptProjection()
    {
        var service = new CapturingPagoService
        {
            Receipt = Receipt()
        };

        var controller = new PagosController(service);

        var result = await controller.GetComprobante(
            1,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var receipt = Assert.IsType<ComprobantePagoDto>(ok.Value);

        Assert.Equal("PAG-000001", receipt.Numero);
        Assert.Equal(30m, receipt.Total);
    }

    [Fact]
    public async Task Annul_UsesAuthenticatedUserAndPaymentId()
    {
        var service = new CapturingPagoService
        {
            AnnulResult =
                PagoOperationResult<PagoDetalleDto>.Success(
                    Detail())
        };

        var controller = Controller(service, "8");

        var result = await controller.Annul(
            15,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PagoDetalleDto>(ok.Value);

        Assert.Equal(1, service.AnnulCalls);
        Assert.Equal(15, service.CapturedAnnulPaymentId);
        Assert.Equal(8, service.CapturedAnnulUserId);
    }

    [Fact]
    public async Task Annul_ReturnsUnauthorizedWhenAuthenticatedUserIdIsInvalid()
    {
        var service = new CapturingPagoService();

        var controller = Controller(service, "invalid");

        var result = await controller.Annul(
            1,
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.AnnulCalls);
        Assert.Null(service.CapturedAnnulUserId);
    }

    [Theory]
    [InlineData(PagoOperationError.Invalid, typeof(BadRequestObjectResult))]
    [InlineData(PagoOperationError.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(PagoOperationError.Conflict, typeof(ConflictObjectResult))]
    public async Task Annul_MapsOperationErrorsToExpectedHttpStatus(
        PagoOperationError error,
        Type expectedResultType)
    {
        var service = new CapturingPagoService
        {
            AnnulResult =
                PagoOperationResult<PagoDetalleDto>.Failure(error)
        };

        var controller = Controller(service, "8");

        var result = await controller.Annul(
            1,
            CancellationToken.None);

        Assert.IsType(expectedResultType, result);
        Assert.Equal(1, service.AnnulCalls);
    }
    private static PagosController Controller(
        CapturingPagoService service,
        string userId)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId)
            },
            "Test");

        return new PagosController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static PagoDetalleDto Detail() =>
        new(
            1,
            "PAG-000001",
            30m,
            DateTime.UtcNow,
            "Pago",
            EstadoPago.Registrado,
            42,
            "admin",
            Titular(),
            new[] { Obligation() });

    private static ComprobantePagoDto Receipt() =>
        new(
            "PAG-000001",
            1,
            DateTime.UtcNow,
            "Pago",
            30m,
            EstadoPago.Registrado.ToString(),
            "admin",
            Titular(),
            new[] { Obligation() });

    private static TitularPagoDto Titular() =>
        new(
            "Suministro",
            1,
            "Juan Pérez",
            "PAN-000001");

    private static ObligacionPagoDto Obligation() =>
        new(
            1,
            OrigenObligacion.CuotaOrdinaria,
            "Cuota ordinaria",
            "2026",
            30m);

    private sealed class CapturingPagoService : IPagoService
    {
        public int? CapturedUserId { get; private set; }

        public int RegisterCalls { get; private set; }

        public int AnnulCalls { get; private set; }

        public int? CapturedAnnulUserId { get; private set; }

        public int? CapturedAnnulPaymentId { get; private set; }

        public PagoOperationResult<PagoDetalleDto> AnnulResult { get; set; } =
            PagoOperationResult<PagoDetalleDto>.Failure(
                PagoOperationError.Invalid);

        public PagoOperationResult<PagoDetalleDto> RegisterResult { get; set; } =
            PagoOperationResult<PagoDetalleDto>.Failure(
                PagoOperationError.Invalid);

        public ComprobantePagoDto? Receipt { get; set; }

        public Task<IReadOnlyList<PagoDto>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PagoDto>>(
                Array.Empty<PagoDto>());

        public Task<PagoDetalleDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PagoDetalleDto?>(null);

        public Task<PagoOperationResult<PagoDetalleDto>> RegisterAsync(
            RegistrarPagoInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            RegisterCalls++;
            CapturedUserId = usuarioAdministrativoId;

            return Task.FromResult(RegisterResult);
        }

        public Task<PagoOperationResult<PagoDetalleDto>> AnnulAsync(
            int id,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            AnnulCalls++;
            CapturedAnnulPaymentId = id;
            CapturedAnnulUserId = usuarioAdministrativoId;

            return Task.FromResult(AnnulResult);
        }
        public Task<ComprobantePagoDto?> GetComprobanteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Receipt);
    }
}
