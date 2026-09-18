using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.AdministracionComite;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/administracion-comite")]
public sealed class AdministracionComiteController : ControllerBase
{
    private readonly IAdministracionComiteService _service;

    public AdministracionComiteController(
        IAdministracionComiteService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AdministracionVer)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AdministracionVer)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(
            id,
            cancellationToken);

        return result is null
            ? NotFound(new { message = "Administración no encontrada." })
            : Ok(result);
    }

    [HttpGet("cargos")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AdministracionVer)]
    public async Task<IActionResult> GetCargos(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetCargosAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AdministracionGestionar)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAdministracionComiteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario autenticado."
            });
        }

        if (request is null ||
            !TryParseDate(request.FechaInicio, out var fechaInicio))
        {
            return BadRequest(new
            {
                message = "Los datos de la administración no son válidos."
            });
        }

        var result = await _service.CreateAsync(
            new CreateAdministracionComiteInput(
                request.Nombre ?? string.Empty,
                fechaInicio),
            actorId,
            cancellationToken);

        return result.Error switch
        {
            AdministracionComiteError.Invalid =>
                BadRequest(new
                {
                    message = "Los datos de la administración no son válidos."
                }),

            AdministracionComiteError.Conflict =>
                Conflict(new
                {
                    message = "Ya existe una administración del comité vigente. Finalízala antes de crear una nueva."
                }),

            _ => CreatedAtAction(
                nameof(GetById),
                new { id = result.Value!.Id },
                result.Value)
        };
    }

    [HttpPut("{id:int}/integrante")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AdministracionGestionar)]
    public async Task<IActionResult> SetIntegrante(
        int id,
        [FromBody] SetIntegranteAdministracionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario autenticado."
            });
        }

        var result = await _service.SetIntegranteAsync(
            id,
            new SetIntegranteAdministracionInput(
                request?.PersonaId ?? 0,
                request?.CargoId ?? 0),
            actorId,
            cancellationToken);

        return result.Error switch
        {
            AdministracionComiteError.Invalid =>
                BadRequest(new
                {
                    message = "La persona o el cargo no son válidos."
                }),

            AdministracionComiteError.NotFound =>
                NotFound(new
                {
                    message = "Administración no encontrada."
                }),

            AdministracionComiteError.Conflict =>
                Conflict(new
                {
                    message = "La administración ya fue finalizada y no puede modificarse."
                }),

            _ => Ok(result.Value)
        };
    }

    [HttpPost("{id:int}/finalizar")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.AdministracionGestionar)]
    public async Task<IActionResult> Finish(
        int id,
        [FromBody] FinishAdministracionComiteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario autenticado."
            });
        }

        if (request is null ||
            !TryParseDate(request.FechaFin, out var fechaFin))
        {
            return BadRequest(new
            {
                message = "La fecha de finalización no es válida."
            });
        }

        var result = await _service.FinishAsync(
            id,
            fechaFin,
            actorId,
            cancellationToken);

        return result.Error switch
        {
            AdministracionComiteError.Invalid =>
                BadRequest(new
                {
                    message = "La fecha de finalización no es válida."
                }),

            AdministracionComiteError.NotFound =>
                NotFound(new
                {
                    message = "Administración no encontrada."
                }),

            AdministracionComiteError.Conflict =>
                Conflict(new
                {
                    message = "La administración ya fue finalizada."
                }),

            _ => Ok(result.Value)
        };
    }

    private static bool TryParseDate(
        string? value,
        out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var claim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        return int.TryParse(claim, out userId) &&
               userId > 0;
    }
}

public sealed class CreateAdministracionComiteRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
}

public sealed class SetIntegranteAdministracionRequest
{
    public int PersonaId { get; set; }
    public int CargoId { get; set; }
}

public sealed class FinishAdministracionComiteRequest
{
    public string FechaFin { get; set; } = string.Empty;
}
