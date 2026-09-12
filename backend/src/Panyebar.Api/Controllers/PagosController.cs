using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Application.Pagos;
using Panyebar.Application.Security;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/pagos")]
public sealed class PagosController : ControllerBase
{
    private readonly IPagoService _service;

    public PagosController(IPagoService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PagosVer)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PagosVer)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var payment = await _service.GetByIdAsync(
            id,
            cancellationToken);

        return payment is null
            ? NotFound(new { message = "Pago no encontrado." })
            : Ok(payment);
    }

    [HttpGet("{id:int}/comprobante")]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PagosVer)]
    public async Task<IActionResult> GetComprobante(
        int id,
        CancellationToken cancellationToken)
    {
        var receipt = await _service.GetComprobanteAsync(
            id,
            cancellationToken);

        return receipt is null
            ? NotFound(new { message = "Pago no encontrado." })
            : Ok(receipt);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:" + AdministrativePermissionCodes.PagosGestionar)]
    public async Task<IActionResult> Register(
        [FromBody] RegistrarPagoInput request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario administrativo autenticado."
            });
        }

        var result = await _service.RegisterAsync(
            request,
            userId,
            cancellationToken);

        return result.Error switch
        {
            PagoOperationError.Invalid =>
                BadRequest(new
                {
                    message = "Los datos del pago no son válidos o el monto no coincide exactamente con las obligaciones seleccionadas."
                }),

            PagoOperationError.NotFound =>
                NotFound(new
                {
                    message = "No se encontró alguna de las obligaciones o recursos requeridos para registrar el pago."
                }),

            PagoOperationError.Conflict =>
                Conflict(new
                {
                    message = "El pago no puede registrarse porque alguna obligación no está pendiente, pertenece a otro titular o ya fue aplicada a otro pago."
                }),

            _ =>
                CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Value!.Id },
                    result.Value)
        };
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(claim, out userId) && userId > 0;
    }
}