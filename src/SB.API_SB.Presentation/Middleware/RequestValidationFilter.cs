using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SB.API_SB.Presentation.Middleware;

/// <summary>
/// Filtro que valida automaticamente los objetos recibidos por los controladores.
/// </summary>
/// <remarks>
/// Para cada argumento de la accion busca en el contenedor un
/// <c>IValidator&lt;T&gt;</c> y, si existe, lo ejecuta. Asi los controladores no
/// contienen codigo de validacion y toda solicitud invalida produce la misma
/// respuesta de error, generada por
/// <see cref="ExceptionHandlingMiddleware"/>.
/// </remarks>
public sealed class RequestValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<RequestValidationFilter> logger;

    public RequestValidationFilter(
        IServiceProvider serviceProvider,
        ILogger<RequestValidationFilter> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        List<ValidationFailure> failures = new();

        foreach (object? argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            IValidator? validator = ResolveValidator(argument.GetType());

            if (validator is null)
            {
                continue;
            }

            ValidationResult validationResult = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            if (!validationResult.IsValid)
            {
                failures.AddRange(validationResult.Errors);
            }
        }

        if (failures.Count > 0)
        {
            logger.LogWarning(
                "Validacion fallida en {ActionName} con {FailureCount} error(es).",
                context.ActionDescriptor.DisplayName,
                failures.Count);

            throw new ValidationException(failures);
        }

        await next();
    }

    private IValidator? ResolveValidator(Type argumentType)
    {
        Type validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

        return serviceProvider.GetService(validatorType) as IValidator;
    }
}
