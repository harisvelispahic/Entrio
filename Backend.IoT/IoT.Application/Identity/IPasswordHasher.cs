namespace IoT.Application.Identity;

public interface IPasswordHasher
{
    /// <summary>Hashes a secret, returning the hash and the freshly generated salt.</summary>
    (string Hash, string Salt) Hash(string password);

    /// <summary>Verifies a candidate secret against a stored hash and salt.</summary>
    bool Verify(string password, string hash, string salt);
}
