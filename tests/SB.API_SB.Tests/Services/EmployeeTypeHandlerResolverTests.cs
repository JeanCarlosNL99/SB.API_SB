using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Tests.TestDoubles;
using Xunit;

namespace SB.API_SB.Tests.Services;

/// <summary>
/// Pruebas del resolutor de manejadores de tipo de empleado, la pieza que hace
/// extensible el modulo sin modificar codigo existente.
/// </summary>
public sealed class EmployeeTypeHandlerResolverTests
{
    private readonly IEmployeeTypeHandlerResolver resolver =
        EmployeeTypeHandlerResolverFactory.Create();

    [Theory]
    [InlineData(EmployeeType.Salaried, "Empleado asalariado")]
    [InlineData(EmployeeType.Hourly, "Empleado por horas")]
    [InlineData(EmployeeType.Commission, "Empleado por comision")]
    [InlineData(EmployeeType.BaseSalariedCommission, "Empleado asalariado por comision")]
    public void Resolve_TipoRegistrado_DevuelveElManejadorCorrecto(
        EmployeeType employeeType,
        string expectedDescription)
    {
        IEmployeeTypeHandler handler = resolver.Resolve(employeeType);

        Assert.Equal(employeeType, handler.HandledType);
        Assert.Equal(expectedDescription, handler.TypeDescription);
    }

    [Fact]
    public void Resolve_TodosLosTiposDelEnumerado_TienenManejadorRegistrado()
    {
        // Si se agrega un tipo al enumerado sin su manejador, esta prueba falla y
        // avisa antes de que el error llegue a ejecucion.
        foreach (EmployeeType employeeType in Enum.GetValues<EmployeeType>())
        {
            IEmployeeTypeHandler handler = resolver.Resolve(employeeType);

            Assert.NotNull(handler);
        }
    }

    [Fact]
    public void Resolve_TipoNoRegistrado_LanzaExcepcionDeReglaDeNegocio()
    {
        const EmployeeType UNREGISTERED_TYPE = (EmployeeType)999;

        Assert.Throws<BusinessRuleViolationException>(() => resolver.Resolve(UNREGISTERED_TYPE));
    }
}
