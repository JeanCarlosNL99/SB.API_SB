using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Domain.Exceptions;

namespace SB.API_SB.Presentation.Middleware;

/// <summary>
/// Manejo centralizado de excepciones.
/// </summary>
/// <remarks>
/// Concentrar el manejo en un middleware evita bloques try/catch repetidos en
/// cada controlador y garantiza que toda respuesta de error tenga el mismo
/// formato (ProblemDetails, RFC 7807). Las excepciones de dominio se traducen a
/// su codigo HTTP correspondiente; cualquier otra se registra completa en el log
/// y se devuelve como error interno generico, sin filtrar la traza al cliente.
/// </remarks>
public sealed class ExceptionHandlingMiddleware
{
    private const string PROBLEM_DETAILS_CONTENT_TYPE = "application/problem+json";
    private const string CORRELATION_IDENTIFIER_KEY = "correlationId";
    private const string ERROR_CODE_KEY = "errorCode";
    private const string VALIDATION_ERRORS_KEY = "errors";
    private const string GENERIC_ERROR_TITLE = "Ocurrio un error inesperado.";
    private const string GENERIC_ERROR_DETAIL =
        "Ocurrio un error inesperado al procesar la solicitud. " +
        "Comunique el identificador de correlacion al administrador del sistema.";

    private readonly RequestDelegate nextMiddleware;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate nextMiddleware,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.nextMiddleware = nextMiddleware;
        this.logger = logger;
    }

    /// <summary>Ejecuta la peticion y captura cualquier excepcion no controlada.</summary>
    /// <param name="httpContext">Contexto de la peticion HTTP.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        try
        {
            await nextMiddleware(httpContext);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        string correlationIdentifier = httpContext.TraceIdentifier;

        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException =>
                BuildValidationProblemDetails(validationException),
            EntityNotFoundException notFoundException => BuildProblemDetails(
                HttpStatusCode.NotFound,
                "Registro no encontrado.",
                notFoundException.Message,
                notFoundException.ErrorCode),
            DuplicatedEntityException duplicatedException => BuildProblemDetails(
                HttpStatusCode.Conflict,
                "Registro duplicado.",
                duplicatedException.Message,
                duplicatedException.ErrorCode),
            InvalidCredentialsException credentialsException => BuildProblemDetails(
                HttpStatusCode.Unauthorized,
                "Credenciales invalidas.",
                credentialsException.Message,
                credentialsException.ErrorCode),
            BusinessRuleViolationException businessRuleException => BuildProblemDetails(
                HttpStatusCode.BadRequest,
                "Regla de negocio incumplida.",
                businessRuleException.Message,
                businessRuleException.ErrorCode),
            OperationCanceledException => BuildProblemDetails(
                HttpStatusCode.BadRequest,
                "Solicitud cancelada.",
                "La solicitud fue cancelada antes de completarse.",
                errorCode: "SOLICITUD_CANCELADA"),
            _ => BuildProblemDetails(
                HttpStatusCode.InternalServerError,
                GENERIC_ERROR_TITLE,
                GENERIC_ERROR_DETAIL,
                errorCode: "ERROR_INTERNO")
        };

        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions[CORRELATION_IDENTIFIER_KEY] = correlationIdentifier;

        LogException(exception, problemDetails, httpContext, correlationIdentifier);

        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning(
                "No se pudo escribir la respuesta de error {CorrelationId}: " +
                "la respuesta ya habia comenzado.",
                correlationIdentifier);

            return;
        }

        httpContext.Response.Clear();
        httpContext.Response.StatusCode =
            problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = PROBLEM_DETAILS_CONTENT_TYPE;

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(
                problemDetails,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
    }

    private void LogException(
        Exception exception,
        ProblemDetails problemDetails,
        HttpContext httpContext,
        string correlationIdentifier)
    {
        bool isExpectedException = exception is DomainException or ValidationException;

        if (isExpectedException)
        {
            logger.LogWarning(
                "Solicitud {Method} {Path} rechazada con codigo {StatusCode}. " +
                "Motivo: {Message}. Correlacion: {CorrelationId}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                problemDetails.Status,
                exception.Message,
                correlationIdentifier);

            return;
        }

        logger.LogError(
            exception,
            "Error no controlado en {Method} {Path}. Correlacion: {CorrelationId}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            correlationIdentifier);
    }

    private static ProblemDetails BuildProblemDetails(
        HttpStatusCode statusCode,
        string title,
        string detail,
        string errorCode)
    {
        ProblemDetails problemDetails = new()
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail
        };

        problemDetails.Extensions[ERROR_CODE_KEY] = errorCode;

        return problemDetails;
    }

    private static ProblemDetails BuildValidationProblemDetails(
        ValidationException validationException)
    {
        Dictionary<string, string[]> errorsByProperty = validationException.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        ProblemDetails problemDetails = BuildProblemDetails(
            HttpStatusCode.BadRequest,
            "Datos invalidos.",
            "Una o mas validaciones no se cumplieron. Revise el detalle de los errores.",
            errorCode: "VALIDACION_FALLIDA");

        problemDetails.Extensions[VALIDATION_ERRORS_KEY] = errorsByProperty;

        return problemDetails;
    }
}
