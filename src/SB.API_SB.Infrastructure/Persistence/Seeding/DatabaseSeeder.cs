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
            IReadOnlyCollection<Company> companies = await SeedCompaniesAsync(cancellationToken);

            await SeedDemonstrationEmployeesAsync(departments, companies, cancellationToken);
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

    private async Task<IReadOnlyCollection<Company>> SeedCompaniesAsync(
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> companiesByTaxIdentification = new()
        {
            ["101-00001-1"] = "Servicios Financieros del Caribe, S. A.",
            ["101-00002-2"] = "Consultoria Tecnologica Quisqueya, SRL",
            ["101-00003-3"] = "Distribuidora Comercial Antillana, S. A."
        };

        List<string> existingTaxIdentifications = await databaseContext.Companies
            .Select(company => company.TaxIdentificationNumber)
            .ToListAsync(cancellationToken);

        foreach ((string taxIdentificationNumber, string name) in companiesByTaxIdentification)
        {
            if (existingTaxIdentifications.Contains(taxIdentificationNumber))
            {
                continue;
            }

            databaseContext.Companies.Add(new Company
            {
                Name = name,
                TaxIdentificationNumber = taxIdentificationNumber,
                IsActive = true,
                CreatedBy = SEED_USER_NAME
            });

            logger.LogInformation("Compania {CompanyName} sembrada.", name);
        }

        await databaseContext.SaveChangesAsync(cancellationToken);

        return await databaseContext.Companies
            .OrderBy(company => company.TaxIdentificationNumber)
            .ToListAsync(cancellationToken);
    }

    private async Task SeedDemonstrationEmployeesAsync(
        IReadOnlyCollection<Department> departments,
        IReadOnlyCollection<Company> companies,
        CancellationToken cancellationToken)
    {
        bool employeesExist = await databaseContext.Employees.AnyAsync(cancellationToken);

        if (employeesExist || departments.Count == 0 || companies.Count == 0)
        {
            return;
        }

        Guid technologyDepartmentId = ResolveDepartmentId(departments, "TIC");
        Guid supervisionDepartmentId = ResolveDepartmentId(departments, "SUP");
        Guid financeDepartmentId = ResolveDepartmentId(departments, "FIN");
        Guid humanResourcesDepartmentId = ResolveDepartmentId(departments, "RRHH");
        Guid legalDepartmentId = ResolveDepartmentId(departments, "LEG");

        Guid financialCompanyId = ResolveCompanyId(companies, "101-00001-1");
        Guid technologyCompanyId = ResolveCompanyId(companies, "101-00002-2");
        Guid distributionCompanyId = ResolveCompanyId(companies, "101-00003-3");

        // Cada compania recibe empleados de los cuatro tipos de contrato, de modo
        // que su nomina semanal ejercite las cuatro formulas de calculo.
        List<Employee> demonstrationEmployees = new()
        {
            new SalariedEmployee
            {
                FirstName = "Ana",
                PaternalLastName = "Martinez",
                SocialSecurityNumber = "001-0000001-1",
                CompanyId = financialCompanyId,
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 35_000m,
                CreatedBy = SEED_USER_NAME
            },
            new HourlyEmployee
            {
                PaternalLastName = "Rodriguez",
                SocialSecurityNumber = "001-0000002-2",
                CompanyId = financialCompanyId,
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
                CompanyId = financialCompanyId,
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
                CompanyId = financialCompanyId,
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
                CompanyId = financialCompanyId,
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Inactive,
                WeeklySalary = 28_000m,
                CreatedBy = SEED_USER_NAME
            },
            new SalariedEmployee
            {
                FirstName = "Patricia",
                PaternalLastName = "Sanchez",
                SocialSecurityNumber = "001-0000006-6",
                CompanyId = technologyCompanyId,
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 42_000m,
                CreatedBy = SEED_USER_NAME
            },
            new HourlyEmployee
            {
                PaternalLastName = "Encarnacion",
                SocialSecurityNumber = "001-0000007-7",
                CompanyId = technologyCompanyId,
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Active,
                HourlyWage = 620m,
                HoursWorked = 44m,
                CreatedBy = SEED_USER_NAME
            },
            new BaseSalariedCommissionEmployee
            {
                FirstName = "Ramon",
                PaternalLastName = "Castillo",
                SocialSecurityNumber = "001-0000008-8",
                CompanyId = technologyCompanyId,
                DepartmentId = humanResourcesDepartmentId,
                Status = EmployeeStatus.Active,
                GrossSales = 95_000m,
                CommissionRate = 0.06m,
                BaseSalary = 25_000m,
                CreatedBy = SEED_USER_NAME
            },
            new CommissionEmployee
            {
                FirstName = "Yolanda",
                PaternalLastName = "Reyes",
                SocialSecurityNumber = "001-0000009-9",
                CompanyId = distributionCompanyId,
                DepartmentId = financeDepartmentId,
                Status = EmployeeStatus.Active,
                GrossSales = 410_000m,
                CommissionRate = 0.045m,
                CreatedBy = SEED_USER_NAME
            },
            new HourlyEmployee
            {
                PaternalLastName = "Montero",
                SocialSecurityNumber = "001-0000010-0",
                CompanyId = distributionCompanyId,
                DepartmentId = supervisionDepartmentId,
                Status = EmployeeStatus.Active,
                HourlyWage = 300m,
                HoursWorked = 52m,
                CreatedBy = SEED_USER_NAME
            },
            new SalariedEmployee
            {
                FirstName = "Hector",
                PaternalLastName = "Bautista",
                SocialSecurityNumber = "001-0000011-1",
                CompanyId = distributionCompanyId,
                DepartmentId = legalDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 31_500m,
                CreatedBy = SEED_USER_NAME
            }
        };

        databaseContext.Employees.AddRange(demonstrationEmployees);

        await databaseContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Se sembraron {EmployeeCount} empleados de demostracion en {CompanyCount} compania(s).",
            demonstrationEmployees.Count,
            companies.Count);
    }

    private static Guid ResolveDepartmentId(
        IReadOnlyCollection<Department> departments,
        string code)
    {
        Department department = departments.FirstOrDefault(item => item.Code == code)
            ?? departments.First();

        return department.Id;
    }

    private static Guid ResolveCompanyId(
        IReadOnlyCollection<Company> companies,
        string taxIdentificationNumber)
    {
        Company company = companies.FirstOrDefault(item =>
            item.TaxIdentificationNumber == taxIdentificationNumber)
            ?? companies.First();

        return company.Id;
    }
}
