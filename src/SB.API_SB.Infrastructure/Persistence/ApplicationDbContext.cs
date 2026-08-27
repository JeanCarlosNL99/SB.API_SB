using Microsoft.EntityFrameworkCore;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.Persistence.Converters;

namespace SB.API_SB.Infrastructure.Persistence;

/// <summary>
/// Contexto de Entity Framework Core de la solucion. Implementa
/// <see cref="IUnitOfWork"/> para que la capa de servicios confirme
/// transacciones sin conocer Entity Framework.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    private const string SYSTEM_USER_NAME = "Sistema";

    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ICurrentUserAccessor? currentUserAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserAccessor? currentUserAccessor = null)
        : base(options)
    {
        this.dateTimeProvider = dateTimeProvider;
        this.currentUserAccessor = currentUserAccessor;
    }

    /// <summary>Empleados, incluidos todos sus subtipos.</summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Ejecuciones de nomina generadas.</summary>
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();

    /// <summary>Lineas de las ejecuciones de nomina.</summary>
    public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();

    /// <summary>Departamentos organizacionales.</summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>Usuarios del sistema.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Roles de seguridad.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Asignaciones de roles a usuarios.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // Todas las configuraciones se descubren por reflexion en este ensamblado,
        // de modo que agregar una entidad no obliga a modificar esta clase.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplyUtcDateTimeConversion(modelBuilder);
    }

    /// <summary>
    /// Aplica a todas las propiedades de fecha del modelo el conversor que las
    /// mantiene en UTC. Recorrer el modelo evita tener que recordar el conversor
    /// en cada entidad nueva.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo de Entity Framework.</param>
    private static void ApplyUtcDateTimeConversion(ModelBuilder modelBuilder)
    {
        UtcDateTimeConverter utcConverter = new();
        NullableUtcDateTimeConverter nullableUtcConverter = new();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ApplyAuditInformation();

        return base.SaveChanges();
    }

    /// <summary>
    /// Completa automaticamente los campos de auditoria de las entidades
    /// modificadas. Centralizarlo aqui garantiza que ningun servicio pueda
    /// olvidarse de registrarlos.
    /// </summary>
    private void ApplyAuditInformation()
    {
        string userName = currentUserAccessor?.UserName ?? SYSTEM_USER_NAME;
        DateTime currentDateTime = dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // Se respeta la fecha que la entidad ya trae asignada. La siembra del
                // historial de nomina fija la fecha en que cada ejecucion se habria
                // generado; sobrescribirla haria que ocho semanas de historico
                // aparecieran creadas en el mismo instante.
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = currentDateTime;
                }

                entry.Entity.CreatedBy = string.IsNullOrWhiteSpace(entry.Entity.CreatedBy)
                    ? userName
                    : entry.Entity.CreatedBy;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = currentDateTime;
                entry.Entity.UpdatedBy = userName;
            }
        }
    }
}
