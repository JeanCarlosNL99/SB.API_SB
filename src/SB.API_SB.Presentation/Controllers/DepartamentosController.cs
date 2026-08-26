using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Contracts.Departments;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Mantenimiento de departamentos. Alimenta el filtro por departamento de la
/// consulta de empleados.
/// </summary>
[ApiController]
[Route("api/departamentos")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
public sealed class DepartamentosController : ControllerBase
{
    private readonly IDepartmentService departmentService;

    public DepartamentosController(IDepartmentService departmentService)
    {
        this.departmentService = departmentService;
    }

    /// <summary>Obtiene todos los departamentos con su cantidad de empleados.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Departamentos registrados.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<DepartmentResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentResponse>>> Consultar(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DepartmentResponse> response = await departmentService.GetAllAsync(
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Obtiene un departamento por su identificador.</summary>
    /// <param name="id">Identificador del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento solicitado.</returns>
    /// <response code="200">Departamento encontrado.</response>
    /// <response code="404">El departamento no existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>> ObtenerPorIdentificador(
        Guid id,
        CancellationToken cancellationToken)
    {
        DepartmentResponse response = await departmentService.GetByIdAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>Registra un nuevo departamento.</summary>
    /// <param name="request">Datos del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento registrado.</returns>
    /// <response code="201">Departamento creado correctamente.</response>
    /// <response code="409">Ya existe un departamento con el mismo codigo.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentResponse>> Crear(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        DepartmentResponse response = await departmentService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorIdentificador),
            new { id = response.Id },
            response);
    }

    /// <summary>Actualiza un departamento existente.</summary>
    /// <param name="id">Identificador del departamento.</param>
    /// <param name="request">Nuevos datos del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento actualizado.</returns>
    /// <response code="200">Departamento actualizado correctamente.</response>
    /// <response code="404">El departamento no existe.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>> Actualizar(
        Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        DepartmentResponse response = await departmentService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Elimina un departamento que no tenga empleados asignados.</summary>
    /// <param name="id">Identificador del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <response code="204">Departamento eliminado correctamente.</response>
    /// <response code="400">El departamento tiene empleados asignados.</response>
    /// <response code="404">El departamento no existe.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ADMINISTRATION_ONLY)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await departmentService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
