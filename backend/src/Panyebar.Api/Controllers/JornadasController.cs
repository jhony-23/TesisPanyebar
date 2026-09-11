using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Jornadas;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/jornadas")]
public sealed class JornadasController : ControllerBase
{
    private readonly IJornadaService _service;

    public JornadasController(IJornadaService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasVer)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasVer)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var jornada = await _service.GetByIdAsync(id, cancellationToken);

        return jornada is null
            ? NotFound(new { message = "Jornada no encontrada." })
            : Ok(jornada);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> Create(
        [FromBody] JornadaInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.CreateAsync(
            request,
            userId,
            cancellationToken);

        return MapMutationResult(
            result,
            created: true,
            createdAction: nameof(GetById));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] JornadaInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.UpdateAsync(
            id,
            request,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    [HttpPost("{id:int}/cancelacion")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelarJornadaInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.CancelAsync(
            id,
            request,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    [HttpPost("{id:int}/participantes")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> AddParticipants(
        int id,
        [FromBody] AgregarParticipantesJornadaInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.AddParticipantsAsync(
            id,
            request,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    [HttpDelete("{id:int}/participantes/{personaId:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> RemoveParticipant(
        int id,
        int personaId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.RemoveParticipantAsync(
            id,
            personaId,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    [HttpPut("{id:int}/participantes/{personaId:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> UpdateParticipant(
        int id,
        int personaId,
        [FromBody] ActualizarParticipacionJornadaInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.UpdateParticipantAsync(
            id,
            personaId,
            request,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    [HttpPost("{id:int}/cierre")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.JornadasGestionar)]
    public async Task<IActionResult> Close(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.CloseAsync(
            id,
            userId,
            cancellationToken);

        return MapMutationResult(result);
    }

    private IActionResult MapMutationResult(
        JornadaOperationResult<JornadaDetalleDto> result,
        bool created = false,
        string? createdAction = null)
    {
        return result.Error switch
        {
            JornadaOperationError.Invalid =>
                BadRequest(new
                {
                    message = "Los datos de la jornada o de la operación no son válidos."
                }),

            JornadaOperationError.NotFound =>
                NotFound(new
                {
                    message = "No se encontró el recurso solicitado."
                }),

            JornadaOperationError.Conflict =>
                Conflict(new
                {
                    message = "La operación no es válida para el estado actual de la jornada."
                }),

            _ when created =>
                CreatedAtAction(
                    createdAction!,
                    new { id = result.Value!.Id },
                    result.Value),

            _ => Ok(result.Value)
        };
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(claim, out userId) && userId > 0;
    }
}
