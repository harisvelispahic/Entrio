using System.Net;

namespace IoT.Application.Common.Exceptions;

/// <summary>
/// Base type for every <b>expected, client-facing</b> error.
///
/// Anything deriving from this is safe to show an API caller: the exception-handling
/// pipeline maps <see cref="StatusCode"/> onto the HTTP response and returns
/// <see cref="Exception.Message"/> verbatim. Infrastructure or otherwise unexpected
/// failures must NOT derive from this — they fall through to the terminal handler,
/// which logs them in full and returns a generic 500 with no internal detail.
/// </summary>
public abstract class EntrioException : Exception
{
    /// <summary>HTTP status the pipeline returns for this error.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Stable, machine-readable category used as the key in the response's
    /// <c>errors</c> dictionary, so clients can branch without parsing prose.
    /// </summary>
    public string ErrorKey { get; }

    protected EntrioException(string message, HttpStatusCode statusCode, string errorKey)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorKey = errorKey;
    }
}

/// <summary>
/// Bad credentials, or a missing/expired/revoked refresh token. Maps to HTTP 401.
/// The message is deliberately vague ("Invalid email or password") so it cannot be
/// used to discover which accounts exist.
/// </summary>
public sealed class UnauthorizedException : EntrioException
{
    public UnauthorizedException(string message)
        : base(message, HttpStatusCode.Unauthorized, "unauthorized") { }
}

/// <summary>A requested entity does not exist. Maps to HTTP 404.</summary>
public sealed class NotFoundException : EntrioException
{
    public NotFoundException(string message)
        : base(message, HttpStatusCode.NotFound, "notFound") { }
}

/// <summary>
/// The request was understood but violates a domain rule (vent percentage out of
/// range, no device registered, scheduling in the past). Maps to HTTP 400.
/// </summary>
public sealed class BusinessRuleException : EntrioException
{
    public BusinessRuleException(string message)
        : base(message, HttpStatusCode.BadRequest, "businessRule") { }
}
