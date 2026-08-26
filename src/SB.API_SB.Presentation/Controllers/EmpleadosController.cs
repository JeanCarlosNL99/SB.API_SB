using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Mantenimiento de empleados. El pago semanal se calcula automaticamente segun
/// el tipo de contrato del empleado.
/// </summary>
[ApiController]
[Route("api/empleados")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
public sealed class EmpleadosController : ControllerBase
{
    private readonly IEmployeeService employeeService;

    public EmpleadosController(IEmployeeService employeeService)
    {
        this.employeeService = employeeService;
    }

    /// <summary>
    /// Consulta empleados con filtros por nombre, departamento, estado y tipo.
    /// </summary>
    /// <param name="filter">Filtros y paginacion solicitados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de empleados con su pago semanal calculado.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EmployeeResponse>>> Consultar(
        [FromQuery] EmployeeFilterRequest filter,
        CancellationToken cancellationToken)
    {
        PagedResponse<EmployeeResponse> response = await employeeService.SearchAsync(
            filter,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene un empleado con el desglose completo del calculo de su pago semanal.
    /// </summary>
    /// <param name="id">Identificador del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado solicitado.</returns>
    /// <response code="200">Empleado encontrado.</response>
    /// <response code="404">El empleado no existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> ObtenerPorIdentificador(
        Guid id,
        CancellationToken cancellationToken)
    {
        EmployeeResponse response = await employeeService.GetByIdAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Registra un nuevo empleado. Los campos exigidos dependen del tipo de
    /// contrato indicado en la propiedad <c>type</c>.
    /// </summary>
    /// <param name="request">Datos del empleado a registrar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado registrado con su pago calculado.</returns>
    /// <response code="201">Empleado creado correctamente.</response>
    /// <response code="400">Los datos enviados no son validos para el tipo indicado.</response>
    /// <response code="409">El numero de seguro social ya esta registrado.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Crear(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        EmployeeResponse response = await employeeService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorIdentificador),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Actualiza un empleado y recalcula su pago semanal con los nuevos valores.
    /// </summary>
    /// <param name="id">Identificador del empleado.</param>
    /// <param name="request">Nuevos datos del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado actualizado con su pago recalculado.</returns>
    /// <response code="200">Empleado actualizado correctamente.</response>
    /// <response code="400">Los datos enviados no son validos o se intento cambiar el tipo.</response>
    /// <response code="404">El empleado no existe.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> Actualizar(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        EmployeeResponse response = await employeeService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Elimina un empleado.</summary>
    /// <param name="id">Identificador del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <response code="204">Empleado eliminado correctamente.</response>
    /// <response code="404">El empleado no existe.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await employeeService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
