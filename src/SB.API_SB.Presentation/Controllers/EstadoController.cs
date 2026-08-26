using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Estado de la API. Permite verificar que el servicio esta disponible sin
/// necesidad de autenticarse.
/// </summary>
[ApiController]
[Route("api/estado")]
[Produces("application/json")]
public sealed class EstadoController : ControllerBase
{
    /// <summary>Devuelve el estado y la version de la API.</summary>
    /// <returns>Estado del servicio.</returns>
    /// <response code="200">El servicio esta disponible.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> ObtenerEstado()
    {
        return Ok(new
        {
            Estado = "Disponible",
            Version = typeof(EstadoController).Assembly.GetName().Version?.ToString(),
            FechaHoraUtc = DateTime.UtcNow
        });
    }
}
