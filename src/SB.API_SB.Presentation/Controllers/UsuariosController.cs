using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Contracts.Users;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Gestion de usuarios y asignacion de roles. Salvo el cambio de la propia
/// contrasena, todas las operaciones requieren el rol administrador.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.ADMINISTRATION_ONLY)]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUserService userService;
    private readonly ICurrentUserAccessor currentUserAccessor;

    public UsuariosController(IUserService userService, ICurrentUserAccessor currentUserAccessor)
    {
        this.userService = userService;
        this.currentUserAccessor = currentUserAccessor;
    }

    /// <summary>Obtiene todos los usuarios con sus roles.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Usuarios registrados.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> Consultar(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserResponse> response = await userService.GetAllAsync(
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Obtiene los roles disponibles en el sistema.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Roles registrados.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet("roles")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> ConsultarRoles(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RoleResponse> response = await userService.GetRolesAsync(
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Obtiene un usuario por su identificador.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario solicitado.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> ObtenerPorIdentificador(
        Guid id,
        CancellationToken cancellationToken)
    {
        UserResponse response = await userService.GetByIdAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>Registra un nuevo usuario y le asigna sus roles.</summary>
    /// <param name="request">Datos del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario registrado.</returns>
    /// <response code="201">Usuario creado correctamente.</response>
    /// <response code="400">Los datos enviados no son validos.</response>
    /// <response code="409">El nombre de usuario o el correo ya estan registrados.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Crear(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        UserResponse response = await userService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorIdentificador),
            new { id = response.Id },
            response);
    }

    /// <summary>Actualiza los datos y los roles de un usuario.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="request">Nuevos datos del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Usuario actualizado correctamente.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Actualizar(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        UserResponse response = await userService.UpdateAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Cambia la contrasena del usuario autenticado. No requiere rol administrador
    /// porque cada usuario administra su propia contrasena.
    /// </summary>
    /// <param name="request">Contrasena actual y nueva.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <response code="204">Contrasena actualizada correctamente.</response>
    /// <response code="400">La nueva contrasena no cumple la politica de seguridad.</response>
    /// <response code="401">La contrasena actual no es correcta.</response>
    [HttpPost("cambiar-contrasena")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CambiarContrasena(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        Guid? userId = currentUserAccessor.UserId;

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        await userService.ChangePasswordAsync(userId.Value, request, cancellationToken);

        return NoContent();
    }

    /// <summary>Elimina un usuario.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <response code="204">Usuario eliminado correctamente.</response>
    /// <response code="400">Un usuario no puede eliminarse a si mismo.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId == id)
        {
            throw new Domain.Exceptions.BusinessRuleViolationException(
                "Un usuario no puede eliminar su propia cuenta.");
        }

        await userService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
