using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace IoT.API.Middleware;

/// <summary>
/// Middle link in the handler chain: turns a FluentValidation <see cref="ValidationException"/>,
/// thrown from the service layer, into an HTTP 400 whose <c>errors</c> dictionary is keyed by
/// property name, so the frontend can show each message under the control it belongs to.
///
/// Registered between the domain handler and the terminal one. Anything that is not a
/// validation failure is passed on (<c>return false</c>).
/// </summary>
public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException ex)
            return false;

        var traceId = httpContext.TraceIdentifier;

        // A rejected request is the caller's problem, not a system fault: Warning, no stack trace.
        logger.LogWarning(
            "Validation failed on {Method} {Path} with {Count} error(s). TraceId: {TraceId}",
            httpContext.Request.Method, httpContext.Request.Path, ex.Errors.Count(), traceId);

        if (httpContext.Response.HasStarted)
            return false;

        // Grouped by property so every message for one field arrives together. Model-level
        // rules carry no property name, so they fall under "request".
        var errors = ex.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        await EntrioExceptionHandler.WriteAsync(httpContext, StatusCodes.Status400BadRequest,
            new ErrorResponse
            {
                Message = ex.Errors.Select(e => e.ErrorMessage).FirstOrDefault()
                          ?? "One or more validation errors occurred.",
                Errors = errors,
                TraceId = traceId
            }, cancellationToken);

        return true;
    }
}
