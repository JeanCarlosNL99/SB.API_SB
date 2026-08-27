using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.Payroll;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>
/// Casos de uso del calculo de pagos semanales.
/// </summary>
/// <remarks>
/// El flujo de trabajo es: consultar la vista previa de la semana, generar la
/// ejecucion y consultarla despues en el historial. La generacion es la unica
/// operacion que persiste datos, y solo se admite una vez por entidad gubernamental y semana.
/// </remarks>
public interface IPayrollRunService
{
    /// <summary>
    /// Calcula la nomina de una semana sin almacenarla, para revisarla antes de
    /// generarla. Informa tambien si la semana ya fue pagada.
    /// </summary>
    /// <param name="governmentEntityId">Entidad gubernamental a calcular.</param>
    /// <param name="year">Ano de la semana.</param>
    /// <param name="weekNumber">Numero de semana.</param>
    /// <param name="onlyActiveEmployees">Indica si se limita a empleados activos.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Calculo propuesto para la semana.</returns>
    Task<PayrollPreviewResponse> PreviewAsync(
        Guid governmentEntityId,
        int year,
        int weekNumber,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera y almacena la nomina de una semana.
    /// </summary>
    /// <param name="request">Entidad gubernamental, semana y alcance del calculo.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion generada, con su detalle.</returns>
    /// <exception cref="Domain.Exceptions.DuplicatedPayrollRunException">
    /// Si la entidad gubernamental ya tiene una ejecucion vigente para esa semana.
    /// </exception>
    Task<PayrollRunDetailResponse> GenerateAsync(
        GeneratePayrollRunRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las entidades gubernamentales que tienen empleados registrados y
    /// por tanto nomina que calcular.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entidades con nomina, ordenadas por nombre.</returns>
    Task<IReadOnlyCollection<PayableGovernmentEntityResponse>> GetPayableEntitiesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Consulta el historial de ejecuciones de nomina.</summary>
    /// <param name="filter">Filtros y paginacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de ejecuciones, de la mas reciente a la mas antigua.</returns>
    Task<PagedResponse<PayrollRunSummaryResponse>> SearchAsync(
        PayrollRunFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene una ejecucion de nomina con su detalle completo.</summary>
    /// <param name="payrollRunId">Identificador de la ejecucion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion solicitada.</returns>
    Task<PayrollRunDetailResponse> GetByIdAsync(
        Guid payrollRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anula una ejecucion de nomina. El documento se conserva como evidencia y la
    /// semana queda libre para volver a calcularse.
    /// </summary>
    /// <param name="payrollRunId">Identificador de la ejecucion.</param>
    /// <param name="request">Motivo de la anulacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion anulada.</returns>
    Task<PayrollRunDetailResponse> CancelAsync(
        Guid payrollRunId,
        CancelPayrollRunRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene las semanas ya pagadas por una entidad gubernamental en un ano.</summary>
    /// <param name="governmentEntityId">Entidad gubernamental consultada.</param>
    /// <param name="year">Ano consultado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Semanas con nomina vigente.</returns>
    Task<GeneratedWeeksResponse> GetGeneratedWeeksAsync(
        Guid governmentEntityId,
        int year,
        CancellationToken cancellationToken = default);
}
