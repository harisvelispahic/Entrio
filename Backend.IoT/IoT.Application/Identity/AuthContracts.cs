namespace IoT.Application.Identity;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

/// <summary>
/// What the client gets on login and on refresh.
///
/// <see cref="AccessTokenExpiresAtUtc"/> is included so the client can refresh proactively
/// instead of waiting to be told "401" mid-action.
/// </summary>
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>A freshly minted access/refresh pair, before the refresh half is persisted.</summary>
public record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshTokenRaw,
    string RefreshTokenHash,
    DateTime RefreshTokenExpiresAtUtc);

public interface ITokenService
{
    /// <summary>Mints an access token plus a fresh random refresh token.</summary>
    TokenPair IssueTokens(Guid ownerId, string email);

    /// <summary>
    /// Hashes a raw refresh token for storage and lookup. The same one-way function is
    /// used on both sides, so the raw value never has to be stored.
    /// </summary>
    string HashRefreshToken(string rawToken);
}
