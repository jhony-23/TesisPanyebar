using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Api.Controllers;
using Panyebar.Application.Jornadas;
using Panyebar.Domain.Enums;

namespace Panyebar.Security.Tests;

public sealed class JornadasControllerSecurityTests
{
    [Theory]
    [InlineData(nameof(JornadasController.GetAll), "Permission:JORNADAS.VER")]
    [InlineData(nameof(JornadasController.GetById), "Permission:JORNADAS.VER")]
    [InlineData(nameof(JornadasController.Create), "Permission:JORNADAS.GESTIONAR")]
    [InlineData(nameof(JornadasController.Update), "Permission:JORNADAS.GESTIONAR")]
    [InlineData(nameof(JornadasController.Cancel), "Permission:JORNADAS.GESTIONAR")]
    [InlineData(nameof(JornadasController.AddParticipants), "Permission:JORNADAS.GESTIONAR")]
    [InlineData(nameof(JornadasController.RemoveParticipant), "Permission:JORNADAS.GESTIONAR")]
    [InlineData(nameof(JornadasController.UpdateParticipant), "Permission:JORNADAS.GESTIONAR")]
    [InlineData(nameof(JornadasController.Close), "Permission:JORNADAS.GESTIONAR")]
    public void Endpoints_RequireExpectedDynamicPermission(
        string action,
        string policy)
    {
        var method = typeof(JornadasController)
            .GetMethod(action, BindingFlags.Instance | BindingFlags.Public);

        var authorize =
            Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(policy, authorize.Policy);
    }

    [Fact]
    public async Task Create_UsesAuthenticatedNameIdentifierAsAdministrativeAuthor()
    {
        var service = new CapturingJornadaService();
        var controller = Controller(service, ClaimTypes.NameIdentifier, "42");

        var result = await controller.Create(Input(), CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(42, service.CapturedUserId);
    }

    [Fact]
    public async Task Close_UsesAuthenticatedSubAsAdministrativeAuthor()
    {
        var service = new CapturingJornadaService();
        var controller = Controller(service, "sub", "73");

        var result = await controller.Close(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(73, service.CapturedUserId);
    }

    [Fact]
    public async Task Mutation_RejectsMissingIdentityWithoutCallingService()
    {
        var service = new CapturingJornadaService();

        var controller = new JornadasController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Cancel(
            1,
            new CancelarJornadaInput("Motivo"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.CapturedUserId);
    }

    private static JornadasController Controller(
        CapturingJornadaService service,
        string claimType,
        string value)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(claimType, value) },
            "Test");

        return new JornadasController(service)
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

    private static JornadaInput Input() => new(
        "Jornada QA",
        null,
        new DateTime(2026, 9, 20),
        new TimeOnly(8, 0),
        new TimeOnly(12, 0),
        null,
        30m);

    private sealed class CapturingJornadaService : IJornadaService
    {
        public int? CapturedUserId { get; private set; }

        public Task<IReadOnlyList<JornadaResumenDto>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<JornadaResumenDto>>(
                Array.Empty<JornadaResumenDto>());

        public Task<JornadaDetalleDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<JornadaDetalleDto?>(Result());

        public Task<JornadaOperationResult<JornadaDetalleDto>> CreateAsync(
            JornadaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        public Task<JornadaOperationResult<JornadaDetalleDto>> UpdateAsync(
            int id,
            JornadaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        public Task<JornadaOperationResult<JornadaDetalleDto>> CancelAsync(
            int id,
            CancelarJornadaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        public Task<JornadaOperationResult<JornadaDetalleDto>> AddParticipantsAsync(
            int id,
            AgregarParticipantesJornadaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        public Task<JornadaOperationResult<JornadaDetalleDto>> RemoveParticipantAsync(
            int id,
            int personaId,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        public Task<JornadaOperationResult<JornadaDetalleDto>> UpdateParticipantAsync(
            int id,
            int personaId,
            ActualizarParticipacionJornadaInput input,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        public Task<JornadaOperationResult<JornadaDetalleDto>> CloseAsync(
            int id,
            int usuarioAdministrativoId,
            CancellationToken cancellationToken = default)
        {
            CapturedUserId = usuarioAdministrativoId;
            return Success();
        }

        private static Task<JornadaOperationResult<JornadaDetalleDto>> Success() =>
            Task.FromResult(
                JornadaOperationResult<JornadaDetalleDto>.Success(Result()));

        private static JornadaDetalleDto Result() => new(
            1,
            "Jornada QA",
            null,
            new DateTime(2026, 9, 20),
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            null,
            30m,
            EstadoJornada.Planificada,
            Array.Empty<ParticipacionJornadaDto>());
    }
}
