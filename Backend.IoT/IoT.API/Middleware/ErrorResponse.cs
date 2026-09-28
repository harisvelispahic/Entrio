namespace IoT.API.Middleware;

/// <summary>
/// The single error shape every failed request returns, so the frontend never has to
/// guess. <see cref="TraceId"/> ties a user-visible failure to the server log entry
/// that carries the real stack trace.
/// </summary>
public sealed class ErrorResponse
{
    public required string Message { get; init; }

    /// <summary>Keyed by <c>EntrioException.ErrorKey</c> ("unauthorized", "notFound", ...).</summary>
    public required Dictionary<string, string[]> Errors { get; init; }

    public required string TraceId { get; init; }
}
