namespace SB.API_SB.Application.Validators;

/// <summary>
/// Limites de longitud y de valor aplicados por las validaciones. Se centralizan
/// como constantes para que coincidan con la configuracion de la base de datos y
/// para eliminar numeros magicos de los validadores.
/// </summary>
public static class ValidationLimits
{
    /// <summary>Longitud maxima de un nombre de persona.</summary>
    public const int PERSON_NAME_MAXIMUM_LENGTH = 100;

    /// <summary>Longitud maxima del numero de seguro social.</summary>
    public const int SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH = 20;

    /// <summary>Longitud minima del numero de seguro social.</summary>
    public const int SOCIAL_SECURITY_NUMBER_MINIMUM_LENGTH = 5;

    /// <summary>Longitud maxima del nombre de una entidad gubernamental.</summary>
    public const int ENTITY_NAME_MAXIMUM_LENGTH = 250;

    /// <summary>Longitud maxima de los campos de clasificacion (categoria, sector, poder).</summary>
    public const int CLASSIFICATION_MAXIMUM_LENGTH = 150;

    /// <summary>Longitud maxima del nombre de un departamento.</summary>
    public const int DEPARTMENT_NAME_MAXIMUM_LENGTH = 150;

    /// <summary>Longitud maxima del codigo de un departamento.</summary>
    public const int DEPARTMENT_CODE_MAXIMUM_LENGTH = 20;

    /// <summary>Longitud maxima de un nombre de usuario.</summary>
    public const int USER_NAME_MAXIMUM_LENGTH = 60;

    /// <summary>Longitud minima de un nombre de usuario.</summary>
    public const int USER_NAME_MINIMUM_LENGTH = 4;

    /// <summary>Longitud maxima de un correo electronico.</summary>
    public const int EMAIL_MAXIMUM_LENGTH = 150;

    /// <summary>Longitud minima exigida a una contrasena.</summary>
    public const int PASSWORD_MINIMUM_LENGTH = 8;

    /// <summary>Longitud maxima admitida para una contrasena.</summary>
    public const int PASSWORD_MAXIMUM_LENGTH = 100;

    /// <summary>Monto maximo admitido en cualquier campo monetario.</summary>
    public const decimal MONETARY_MAXIMUM_VALUE = 10_000_000m;
}
