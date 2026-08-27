using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>Operaciones de persistencia del historial de ejecuciones de nomina.</summary>
public interface IPayrollRunRepository : IRepository<PayrollRun>
{
    /// <summary>
    /// Busca ejecuciones de nomina aplicando filtros y paginacion. Devuelve solo
    /// la cabecera de cada documento, sin sus lineas.
    /// </summary>
    /// <param name="criteria">Criterios de busqueda.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de ejecuciones ordenadas de la mas reciente a la mas antigua.</returns>
    Task<PagedList<PayrollRun>> SearchAsync(
        PayrollRunFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una ejecucion con su detalle completo: lineas, componentes del
    /// calculo y compania.
    /// </summary>
    /// <param name="payrollRunId">Identificador de la ejecucion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion con su detalle o nulo si no existe.</returns>
    Task<PayrollRun?> GetWithDetailAsync(
        Guid payrollRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determina si la compania ya tiene una ejecucion vigente para la semana
    /// indicada. Es la comprobacion que impide pagar dos veces la misma semana.
    /// </summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="payrollWeek">Semana a verificar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion existente, o nulo si la semana esta libre.</returns>
    Task<PayrollRun?> FindGeneratedRunAsync(
        Guid companyId,
        PayrollWeek payrollWeek,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los numeros de semana ya pagados por una compania en un ano, para
    /// que la interfaz pueda mostrarlos sin recorrer el historial completo.
    /// </summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="year">Ano consultado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Numeros de semana con ejecucion vigente.</returns>
    Task<IReadOnlyCollection<int>> GetGeneratedWeekNumbersAsync(
        Guid companyId,
        int year,
        CancellationToken cancellationToken = default);
}
