using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.Persistence.Seeding;

/// <summary>
/// Siembra los datos minimos que la aplicacion necesita para funcionar: roles,
/// usuario administrador, departamentos y, opcionalmente, empleados de
/// demostracion que ejercitan los cuatro tipos de calculo de nomina y quedan
/// asignados a entidades gubernamentales del listado oficial.
/// </summary>
/// <remarks>
/// Cada paso es idempotente: comprueba si el dato ya existe antes de insertarlo,
/// de modo que ejecutar la siembra en cada arranque no duplica registros.
/// </remarks>
public sealed class DatabaseSeeder
{
    private const string SEED_USER_NAME = "Semilla";

    /// <summary>
    /// Entidades gubernamentales del listado oficial a las que se asignan los
    /// empleados de demostracion. Se escriben sin acentos a proposito: la
    /// comparacion los ignora.
    /// </summary>
    private static readonly IReadOnlyList<string> DEMONSTRATION_ENTITY_NAMES = new[]
    {
        "Direccion General de Impuestos Internos",
        "Ministerio de Hacienda y Economia",
        "Oficina Gubernamental de Tecnologias de la Informacion y Comunicacion",
        "Superintendencia del Mercado de Valores"
    };

    private readonly ApplicationDbContext databaseContext;
    private readonly IGovernmentEntityRepository governmentEntityRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly SeedOptions options;
    private readonly ILogger<DatabaseSeeder> logger;

    public DatabaseSeeder(
        ApplicationDbContext databaseContext,
        IGovernmentEntityRepository governmentEntityRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.databaseContext = databaseContext;
        this.governmentEntityRepository = governmentEntityRepository;
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
            IReadOnlyList<GovernmentEntity> governmentEntities =
                await ResolveDemonstrationEntitiesAsync(cancellationToken);

            await SeedDemonstrationEmployeesAsync(
                departments,
                governmentEntities,
                cancellationToken);
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

    /// <summary>
    /// Resuelve, contra el listado oficial, las entidades gubernamentales a las que
    /// se asignan los empleados de demostracion.
    /// </summary>
    /// <remarks>
    /// Las entidades no se crean: ya existen en el archivo de texto plano. Aqui
    /// solo se localizan las que se van a usar. Los nombres buscados se escriben
    /// sin acentos y la comparacion los ignora, de modo que el codigo fuente se
    /// mantiene en ASCII sin dejar de coincidir con el listado oficial, que si los
    /// lleva.
    /// </remarks>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entidades encontradas, en el orden en que se declararon.</returns>
    private async Task<IReadOnlyList<GovernmentEntity>> ResolveDemonstrationEntitiesAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GovernmentEntity> catalog =
            await governmentEntityRepository.GetAllAsync(cancellationToken);

        Dictionary<string, GovernmentEntity> entitiesByComparableName = catalog
            .GroupBy(entity => BuildComparableName(entity.Name))
            .ToDictionary(group => group.Key, group => group.First());

        List<GovernmentEntity> resolvedEntities = new(DEMONSTRATION_ENTITY_NAMES.Count);

        foreach (string entityName in DEMONSTRATION_ENTITY_NAMES)
        {
            if (entitiesByComparableName.TryGetValue(
                    BuildComparableName(entityName),
                    out GovernmentEntity? entity))
            {
                resolvedEntities.Add(entity);
                continue;
            }

            logger.LogWarning(
                "La entidad gubernamental {EntityName} no esta en el listado oficial. " +
                "No se le asignaran empleados de demostracion.",
                entityName);
        }

        return resolvedEntities;
    }

    private async Task SeedDemonstrationEmployeesAsync(
        IReadOnlyCollection<Department> departments,
        IReadOnlyList<GovernmentEntity> governmentEntities,
        CancellationToken cancellationToken)
    {
        bool employeesExist = await databaseContext.Employees.AnyAsync(cancellationToken);

        if (employeesExist || departments.Count == 0 || governmentEntities.Count == 0)
        {
            return;
        }

        Guid technologyDepartmentId = ResolveDepartmentId(departments, "TIC");
        Guid supervisionDepartmentId = ResolveDepartmentId(departments, "SUP");
        Guid financeDepartmentId = ResolveDepartmentId(departments, "FIN");
        Guid humanResourcesDepartmentId = ResolveDepartmentId(departments, "RRHH");
        Guid legalDepartmentId = ResolveDepartmentId(departments, "LEG");

        Guid taxAdministrationEntityId = ResolveGovernmentEntityId(governmentEntities, 0);
        Guid treasuryEntityId = ResolveGovernmentEntityId(governmentEntities, 1);
        Guid technologyEntityId = ResolveGovernmentEntityId(governmentEntities, 2);
        Guid securitiesEntityId = ResolveGovernmentEntityId(governmentEntities, 3);

        // Los empleados se reparten entre las entidades resueltas y cubren los
        // cuatro tipos de contrato, de modo que la nomina semanal de la
        // demostracion ejercite las cuatro formulas de calculo.
        List<Employee> demonstrationEmployees = new()
        {
            new SalariedEmployee
            {
                FirstName = "Ana",
                PaternalLastName = "Martinez",
                SocialSecurityNumber = "001-0000001-1",
                GovernmentEntityId = taxAdministrationEntityId,
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 35_000m,
                CreatedBy = SEED_USER_NAME
            },
            new HourlyEmployee
            {
                PaternalLastName = "Rodriguez",
                SocialSecurityNumber = "001-0000002-2",
                GovernmentEntityId = taxAdministrationEntityId,
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
                GovernmentEntityId = taxAdministrationEntityId,
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
                GovernmentEntityId = treasuryEntityId,
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
                GovernmentEntityId = treasuryEntityId,
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
                GovernmentEntityId = treasuryEntityId,
                DepartmentId = technologyDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 42_000m,
                CreatedBy = SEED_USER_NAME
            },
            new HourlyEmployee
            {
                PaternalLastName = "Encarnacion",
                SocialSecurityNumber = "001-0000007-7",
                GovernmentEntityId = technologyEntityId,
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
                GovernmentEntityId = technologyEntityId,
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
                GovernmentEntityId = securitiesEntityId,
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
                GovernmentEntityId = securitiesEntityId,
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
                GovernmentEntityId = securitiesEntityId,
                DepartmentId = legalDepartmentId,
                Status = EmployeeStatus.Active,
                WeeklySalary = 31_500m,
                CreatedBy = SEED_USER_NAME
            }
        };

        databaseContext.Employees.AddRange(demonstrationEmployees);

        await databaseContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Se sembraron {EmployeeCount} empleados de demostracion repartidos entre " +
            "{GovernmentEntityCount} entidad(es) gubernamental(es).",
            demonstrationEmployees.Count,
            governmentEntities.Count);
    }

    private static Guid ResolveDepartmentId(
        IReadOnlyCollection<Department> departments,
        string code)
    {
        Department department = departments.FirstOrDefault(item => item.Code == code)
            ?? departments.First();

        return department.Id;
    }

    /// <summary>
    /// Obtiene el identificador de la entidad gubernamental en la posicion
    /// indicada de las resueltas, ajustando la posicion si se resolvieron menos de
    /// las esperadas. Asi la siembra funciona incluso si el listado oficial cambia.
    /// </summary>
    /// <param name="governmentEntities">Entidades resueltas.</param>
    /// <param name="position">Posicion deseada.</param>
    /// <returns>Identificador de la entidad asignada.</returns>
    private static Guid ResolveGovernmentEntityId(
        IReadOnlyList<GovernmentEntity> governmentEntities,
        int position) =>
        governmentEntities[position % governmentEntities.Count].Id;

    /// <summary>
    /// Normaliza un nombre para compararlo: descompone los caracteres acentuados y
    /// descarta las marcas diacriticas, dejando solo las letras base en
    /// mayusculas.
    /// </summary>
    /// <param name="name">Nombre a normalizar.</param>
    /// <returns>Nombre comparable, sin acentos y en mayusculas.</returns>
    private static string BuildComparableName(string name)
    {
        string decomposedName = name.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder comparableName = new(decomposedName.Length);

        foreach (char character in decomposedName)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                comparableName.Append(character);
            }
        }

        return comparableName.ToString().ToUpperInvariant();
    }
}
