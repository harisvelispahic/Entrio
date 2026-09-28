using Microsoft.AspNetCore.Diagnostics;

namespace IoT.API.Middleware;

/// <summary>
/// Terminal link in the chain: the catch-all for anything the specific handlers did not
/// claim. Every such exception is an unexpected failure, so it is logged in full — stack
/// trace included — and reported to the client as a generic 500 carrying no internal
/// detail in any environment, development included. A developer reads the real cause from
/// the log entry the <c>traceId</c> points at.
///
/// This is what stops a raw EF or SQL exception from reaching a browser, which is exactly
/// what used to happen on a failed login.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string SafeMessage = "An unexpected error occurred. Please try again later.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        logger.LogError(exception,
            "Unhandled exception on {Method} {Path}. TraceId: {TraceId}",
            httpContext.Request.Method, httpContext.Request.Path, traceId);

        if (httpContext.Response.HasStarted)
            return false;

        await EntrioExceptionHandler.WriteAsync(httpContext, StatusCodes.Status500InternalServerError,
            new ErrorResponse
            {
                Message = SafeMessage,
                Errors = new Dictionary<string, string[]> { ["server"] = [SafeMessage] },
                TraceId = traceId
            }, cancellationToken);

        return true; // nothing bubbles past the end of the chain
    }
}
