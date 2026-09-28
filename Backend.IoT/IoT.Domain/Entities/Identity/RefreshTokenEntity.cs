namespace IoT.Domain.Entities.Identity;

/// <summary>
/// A stored refresh token.
///
/// The raw token is handed to the client once and never persisted — only its SHA-256
/// hash lives here, so a database leak cannot be replayed. Using a token rotates it:
/// the presented token is revoked and a replacement issued, which means a stolen token
/// can be used at most once before the theft becomes visible (the real user's next
/// refresh fails). Logging out deletes every token for the account.
/// </summary>
public class RefreshTokenEntity
{
    public Guid Id { get; private set; }
    public Guid OwnerAccountId { get; private set; }

    /// <summary>SHA-256 of the raw token, base64url-encoded. Never the token itself.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private RefreshTokenEntity() { }

    public RefreshTokenEntity(Guid ownerAccountId, string tokenHash, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        OwnerAccountId = ownerAccountId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        IsRevoked = false;
    }

    /// <summary>True when the token is neither revoked nor past its expiry.</summary>
    public bool IsActive(DateTime nowUtc) => !IsRevoked && ExpiresAtUtc > nowUtc;

    public void Revoke()
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
        RevokedAtUtc = DateTime.UtcNow;
    }
}
