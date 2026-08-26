using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.Persistence.Seeding;

/// <summary>
/// Siembra los datos minimos que la aplicacion necesita para funcionar: roles,
/// usuario administrador, departamentos y, opcionalmente, empleados de
/// demostracion que ejercitan los cuatro tipos de calculo de nomina.
/// </summary>
/// <remarks>
/// Cada paso es idempotente: comprueba si el dato ya existe antes de insertarlo,
/// de modo que ejecutar la siembra en cada arranque no duplica registros.
/// </remarks>
public sealed class DatabaseSeeder
{
    private const string SEED_USER_NAME = "Semilla";

    private readonly ApplicationDbContext databaseContext;
    private readonly IPasswordHasher passwordHasher;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly SeedOptions options;
    private readonly ILogger<DatabaseSeeder> logger;

    public DatabaseSeeder(
        ApplicationDbContext databaseContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.databaseContext = databaseContext;
        this.passwordHasher = passwordHasher;
        this.dateTimeProvider = dateTimeProvider;
        this.options = options.Value;
        this.logger = logger;
    }

    /// <summary>Ejecuta la siembra completa.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Role> roles = await SeedRolesAsync(cancellationToken);

        await SeedAdministratorUserAsync(roles, cancellationToken);

        IReadOnlyCollection<Department> departments = await SeedDepartmentsAsync(cancellationToken);

        if (options.CreateDemonstrationData)
        {
            await SeedDemonstrationEmployeesAsync(departments, cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<Role>> SeedRolesAsync(
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> roleDescriptions = new()
        {
            [RoleNames.ADMINISTRATOR] =
                "Acceso total al sistema, incluida la administracion de usuarios.",
            [RoleNames.HUMAN_RESOURCES] =
                "Gestiona empleados, entidades gubernamentales y reportes de nomina.",
            [RoleNames.CONSULTANT] =
                "Acceso de solo lectura a los mantenimientos y reportes."
        };

        List<string> existingRoleNames = await databaseContext.Roles
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);

        foreach ((string roleName, string description) in roleDescriptions)
        {
            if (existingRoleNames.Contains(roleName))
            {
                continue;
            }

            databaseContext.Roles.Add(new Role
            {
                Name = roleName,
                Description = description,
                CreatedBy = SEED_USER_NAME
            });

            logger.LogInformation("Rol {RoleName} sembrado.", roleName);
        }

        await databaseContext.SaveChangesAsync(cancellationToken);

        return await databaseContext.Roles.ToListAsync(cancellationToken);
    }

    private async Task SeedAdministratorUserAsync(
        IReadOnlyCollection<Role> roles,
        CancellationToken cancellationToken)
    {
        bool administratorExists = await databaseContext.Users
            .AnyAsync(user => user.UserName == options.AdministratorUserName, cancellationToken);

        if (administratorExists)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.AdministratorPassword))
        {
            logger.LogError(
                "No se sembro el usuario administrador porque no hay contrasena configurada " +
                "en la seccion {SectionName} de AppSettings.",
                SeedOptions.SECTION_NAME);

            return;
        }

        (string hash, string salt) = passwordHasher.HashPassword(options.AdministratorPassword);

        User administrator = new()
        {
            UserName = options.AdministratorUserName,
            Email = options.AdministratorEmail,
            FullName = options.AdministratorFullName,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedBy = SEED_USER_NAME
        };

        Role? administratorRole = roles.FirstOrDefault(role =>
            role.Name == RoleNames.ADMINISTRATOR);

        if (administratorRole is not null)
        {
            administrator.UserRoles.Add(new UserRole
            {
                RoleId = administratorRole.Id,
                AssignedAt = dateTimeProvider.UtcNow
            });
        }

        databaseContext.Users.Add(administrator);

        await databaseContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Usuario administrador {UserName} sembrado.",
            options.AdministratorUserName);
    }

    private async Task<IReadOnlyCollection<Department>> SeedDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> departmentsByCode = new()
        {
            ["TIC"] = "Tecnologia de la Informacion",
            ["RRHH"] = "Recursos Humanos",
            ["FIN"] = "Finanzas",
            ["SUP"] = "Supervision Bancaria",
            ["LEG"] = "Consultoria Juridica"
        };

        List<string> existingCodes = await databaseContext.Departments
            .Select(department => department.Code)
            .ToListAsync(cancellationToken);

        foreach ((string code, string name) in departmentsByCode)
        {
            if (existingCodes.Contains(code))
            {
                continue;
            }

            databaseContext.Departments.Add(new Department
            {
                Code = code,
                Name = name,
                IsActive = true,
                CreatedBy = SEED_USER_NAME
            });
        }

        await databaseContext.SaveChangesAsync(cancellationToken);

        return await databaseContext.Departments.ToListAsync(cancellationToken);
    }

    private async Task SeedDemonstrationEmployeesAsync(
        IReadOnlyCollection<Department> departments,
        CancellationToken cancellationToken)
    {
        bool employeesExist = await databaseContext.Employees.AnyAsync(cancellationToken);

        if (employeesExist || departments.Count == 0)
        {
            return;
        }

        Guid technologyDepartmentId = ResolveDepartmentId(departments, "TIC");
        Guid supervisionDepartmentId = ResolveDepartmentId(departments, "SUP");
        Guid financeDepartmentId = ResolveDepartmentId(departments, "FIN");

        List<Employee> demonstrationEmployees = new()
        {
            new SalariedEmployee
            {
                FirstName = "Ana",
                PaternalLastName = "Martinez",
                SocialSecurityNumber = "001-0000001-1",
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 35_000m,
                CreatedBy = SEED_USER_NAME
            },
            new HourlyEmployee
            {
                PaternalLastName = "Rodriguez",
                SocialSecurityNumber = "001-0000002-2",
                DepartmentId = supervisionDepartmentId,
                Status = EmployeeStatus.Active,
                HourlyWage = 450m,
                HoursWorked = 46m,
                CreatedBy = SEED_USER_NAME
            },
            new CommissionEmployee
            {
                FirstName = "Luis",
                PaternalLastName = "Perez",
                SocialSecurityNumber = "001-0000003-3",
                DepartmentId = financeDepartmentId,
                Status = EmployeeStatus.Active,
                GrossSales = 250_000m,
                CommissionRate = 0.08m,
                CreatedBy = SEED_USER_NAME
            },
            new BaseSalariedCommissionEmployee
            {
                FirstName = "Carmen",
                PaternalLastName = "Guzman",
                SocialSecurityNumber = "001-0000004-4",
                DepartmentId = financeDepartmentId,
                Status = EmployeeStatus.Active,
                GrossSales = 180_000m,
                CommissionRate = 0.05m,
                BaseSalary = 20_000m,
                CreatedBy = SEED_USER_NAME
            },
            new SalariedEmployee
            {
                FirstName = "Jose",
                PaternalLastName = "Fernandez",
                SocialSecurityNumber = "001-0000005-5",
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Inactive,
                WeeklySalary = 28_000m,
                CreatedBy = SEED_USER_NAME
            }
        };

        databaseContext.Employees.AddRange(demonstrationEmployees);

        await databaseContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Se sembraron {EmployeeCount} empleados de demostracion.",
            demonstrationEmployees.Count);
    }

    private static Guid ResolveDepartmentId(
        IReadOnlyCollection<Department> departments,
        string code)
    {
        Department department = departments.FirstOrDefault(item => item.Code == code)
            ?? departments.First();

        return department.Id;
    }
}
