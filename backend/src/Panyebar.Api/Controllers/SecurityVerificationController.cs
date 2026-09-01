using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Panyebar.Api.Controllers;

[ApiController]
[Route("api/security")]
public sealed class SecurityVerificationController : ControllerBase
{
    [HttpGet("administrative")]
    [Authorize(Policy = "Permission:SEGURIDAD.PRUEBA")]
    public IActionResult Administrative()
    {
        return Ok(new
        {
            authenticated = true,
            message = "Acceso administrativo autenticado."
        });
    }
}
