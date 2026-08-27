using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Contracts.Companies;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Mantenimiento de companias. Cada empleado pertenece a una compania y cada
/// nomina se calcula para una compania y una semana.
/// </summary>
[ApiController]
[Route("api/companias")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
public sealed class CompaniasController : ControllerBase
{
    private readonly ICompanyService companyService;

    public CompaniasController(ICompanyService companyService)
    {
        this.companyService = companyService;
    }

    /// <summary>Obtiene todas las companias con su cantidad de empleados activos.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Companias registradas.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CompanyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CompanyResponse>>> Consultar(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CompanyResponse> response = await companyService.GetAllAsync(
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Obtiene una compania por su identificador.</summary>
    /// <param name="id">Identificador de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania solicitada.</returns>
    /// <response code="200">Compania encontrada.</response>
    /// <response code="404">La compania no existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyResponse>> ObtenerPorIdentificador(
        Guid id,
        CancellationToken cancellationToken)
    {
        CompanyResponse response = await companyService.GetByIdAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>Registra una nueva compania.</summary>
    /// <param name="request">Datos de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania registrada.</returns>
    /// <response code="201">Compania creada correctamente.</response>
    /// <response code="400">Los datos enviados no son validos.</response>
    /// <response code="409">Ya existe una compania con el mismo registro.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyResponse>> Crear(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        CompanyResponse response = await companyService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorIdentificador),
            new { id = response.Id },
            response);
    }

    /// <summary>Actualiza una compania existente.</summary>
    /// <param name="id">Identificador de la compania.</param>
    /// <param name="request">Nuevos datos de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania actualizada.</returns>
    /// <response code="200">Compania actualizada correctamente.</response>
    /// <response code="404">La compania no existe.</response>
    /// <response code="409">Ya existe otra compania con el mismo registro.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyResponse>> Actualizar(
        Guid id,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        CompanyResponse response = await companyService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Elimina una compania sin empleados ni nominas registradas.</summary>
    /// <param name="id">Identificador de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <response code="204">Compania eliminada correctamente.</response>
    /// <response code="400">La compania tiene empleados o nominas asociadas.</response>
    /// <response code="404">La compania no existe.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ADMINISTRATION_ONLY)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await companyService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
