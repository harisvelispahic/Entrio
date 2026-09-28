namespace IoT.Domain.Entities.Identity;

/// <summary>
/// A stored refresh token.
///
/// The raw token is handed to the client once and never persisted — only its SHA-256 hash
/// lives here, so a database leak cannot be replayed. Using a token rotates it: the
/// presented token is revoked and a replacement issued, which means a stolen token works
/// at most once before the theft becomes visible. Logging out revokes every token for the
/// account.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid OwnerAccountId { get; set; }

    /// <summary>SHA-256 of the raw token, base64url-encoded. Never the token itself.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Neither revoked nor past its expiry. Kept on the entity because it is a
    /// fact about the row, not a policy decision.</summary>
    public bool IsActive(DateTime nowUtc) => !IsRevoked && ExpiresAtUtc > nowUtc;
}
