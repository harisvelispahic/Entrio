namespace IoT.Domain.Entities.Identity;

/// <summary>
/// The single account that can use the web UI. There is no registration flow: the row is
/// seeded with public demo credentials so anyone cloning the repository can log in.
/// </summary>
public class OwnerAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    /// <summary>PBKDF2-HMAC-SHA256 hash. The plaintext is never stored or logged.</summary>
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
