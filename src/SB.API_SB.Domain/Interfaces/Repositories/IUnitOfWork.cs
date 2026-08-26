namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>
/// Unidad de trabajo. Agrupa varias operaciones de repositorio en una sola
/// transaccion, de modo que la capa de servicios decide cuando confirmar los
/// cambios sin conocer el ORM utilizado.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Confirma en el almacen de datos todos los cambios pendientes.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Cantidad de registros afectados.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
