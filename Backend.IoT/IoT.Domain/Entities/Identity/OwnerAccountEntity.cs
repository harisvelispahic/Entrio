namespace IoT.Domain.Entities.Identity;

public class OwnerAccountEntity
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string PasswordSalt { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    private OwnerAccountEntity() { }

    public OwnerAccountEntity(string email, string passwordHash, string passwordSalt)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }
}
