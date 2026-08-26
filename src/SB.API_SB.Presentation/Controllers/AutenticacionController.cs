using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Contracts.Authentication;
using SB.API_SB.Application.Interfaces.Services;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Autenticacion de usuarios y emision del token de acceso.
/// </summary>
[ApiController]
[Route("api/autenticacion")]
[Produces("application/json")]
public sealed class AutenticacionController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;

    public AutenticacionController(IAuthenticationService authenticationService)
    {
        this.authenticationService = authenticationService;
    }

    /// <summary>
    /// Valida las credenciales del usuario y devuelve un token JWT.
    /// </summary>
    /// <param name="request">Nombre de usuario y contrasena.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Token de acceso, su vigencia y los roles del usuario.</returns>
    /// <response code="200">Autenticacion exitosa.</response>
    /// <response code="400">Los datos enviados no son validos.</response>
    /// <response code="401">Las credenciales no son validas.</response>
    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> IniciarSesion(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthenticationResponse response = await authenticationService.LoginAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Devuelve la identidad del usuario asociada al token enviado. Sirve para que
    /// el cliente valide que su sesion sigue vigente.
    /// </summary>
    /// <returns>Nombre de usuario y roles del token actual.</returns>
    /// <response code="200">El token es valido.</response>
    /// <response code="401">No se envio un token valido.</response>
    [HttpGet("sesion-actual")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> ObtenerSesionActual()
    {
        return Ok(new
        {
            UserName = User.FindFirst("userName")?.Value ?? User.Identity?.Name,
            Roles = User.Claims
                .Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray()
        });
    }
}
