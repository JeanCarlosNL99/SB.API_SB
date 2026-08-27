using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de la jerarquia de empleados.
/// </summary>
/// <remarks>
/// Se utiliza la estrategia Table Per Hierarchy (una sola tabla con columna
/// discriminadora). Es la mas eficiente para este caso porque el reporte de
/// nomina recorre todos los tipos de empleado a la vez y con TPH se resuelve con
/// una unica consulta, sin uniones adicionales.
/// </remarks>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <summary>Nombre de la columna discriminadora del tipo de empleado.</summary>
    public const string DISCRIMINATOR_COLUMN_NAME = "EmployeeType";

    private const int PERSON_NAME_MAXIMUM_LENGTH = 100;
    private const int SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH = 20;
    private const int AUDIT_USER_MAXIMUM_LENGTH = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Employees");

        builder.HasKey(employee => employee.Id);

        // Propiedades calculadas por el dominio: no se persisten.
        builder.Ignore(employee => employee.Type);
        builder.Ignore(employee => employee.FullName);

        builder.Property(employee => employee.FirstName)
            .HasMaxLength(PERSON_NAME_MAXIMUM_LENGTH);

        builder.Property(employee => employee.PaternalLastName)
            .IsRequired()
            .HasMaxLength(PERSON_NAME_MAXIMUM_LENGTH);

        builder.Property(employee => employee.SocialSecurityNumber)
            .IsRequired()
            .HasMaxLength(SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH);

        builder.Property(employee => employee.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(employee => employee.CreatedBy)
            .IsRequired()
            .HasMaxLength(AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(employee => employee.UpdatedBy)
            .HasMaxLength(AUDIT_USER_MAXIMUM_LENGTH);

        // El numero de seguro social identifica al empleado: se protege con un
        // indice unico en la base de datos, no solo con una validacion.
        builder.HasIndex(employee => employee.SocialSecurityNumber)
            .IsUnique()
            .HasDatabaseName("IX_Employees_SocialSecurityNumber");

        // Indices que respaldan los filtros expuestos por la API.
        builder.HasIndex(employee => employee.PaternalLastName)
            .HasDatabaseName("IX_Employees_PaternalLastName");

        builder.HasIndex(employee => new { employee.DepartmentId, employee.Status })
            .HasDatabaseName("IX_Employees_DepartmentId_Status");

        // Indice que respalda tanto el filtro por entidad gubernamental de la
        // consulta como la seleccion de empleados al calcular la nomina semanal.
        //
        // No hay clave foranea hacia la entidad gubernamental, y no puede haberla:
        // el listado oficial vive en el archivo de texto plano y una base de datos
        // relacional no puede imponer integridad referencial contra un almacen que
        // no administra. La comprobacion se hace en la capa de servicios, que
        // valida contra el catalogo antes de aceptar el empleado.
        builder.HasIndex(employee => new { employee.GovernmentEntityId, employee.Status })
            .HasDatabaseName("IX_Employees_GovernmentEntityId_Status");

        builder.HasOne(employee => employee.Department)
            .WithMany(department => department.Employees)
            .HasForeignKey(employee => employee.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasDiscriminator<int>(DISCRIMINATOR_COLUMN_NAME)
            .HasValue<SalariedEmployee>((int)EmployeeType.Salaried)
            .HasValue<HourlyEmployee>((int)EmployeeType.Hourly)
            .HasValue<CommissionEmployee>((int)EmployeeType.Commission)
            .HasValue<BaseSalariedCommissionEmployee>((int)EmployeeType.BaseSalariedCommission);
    }
}
