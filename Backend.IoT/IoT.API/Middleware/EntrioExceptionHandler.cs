using System.Text.Json;
using IoT.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace IoT.API.Middleware;

/// <summary>
/// First link in the handler chain: deals with the <see cref="EntrioException"/>
/// hierarchy — the application's own expected, client-facing errors.
///
/// Each exception carries its own status code and error key, so one handler covers
/// Unauthorized / NotFound / BusinessRule without a per-type branch. Anything it does
/// not recognise is passed on (<c>return false</c>) to the next handler, which is the
/// equivalent of a <c>catch (EntrioException)</c> block.
/// </summary>
public sealed class EntrioExceptionHandler(ILogger<EntrioExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not EntrioException ex)
            return false; // not ours — let the terminal handler take it

        var traceId = httpContext.TraceIdentifier;

        // An expected domain error, not a system fault: Warning, not Error, and no stack trace.
        logger.LogWarning(
            "Handled {ExceptionType} ({StatusCode}) on {Method} {Path}. TraceId: {TraceId}. {Message}",
            ex.GetType().Name, (int)ex.StatusCode, httpContext.Request.Method,
            httpContext.Request.Path, traceId, ex.Message);

        if (httpContext.Response.HasStarted)
            return false;

        await WriteAsync(httpContext, (int)ex.StatusCode, new ErrorResponse
        {
            Message = ex.Message,
            Errors = new Dictionary<string, string[]> { [ex.ErrorKey] = [ex.Message] },
            TraceId = traceId
        }, cancellationToken);

        return true;
    }

    internal static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        ErrorResponse body,
        CancellationToken ct)
    {
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }),
            ct);
    }
}
