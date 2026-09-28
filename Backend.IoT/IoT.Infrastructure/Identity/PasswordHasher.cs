using System.Security.Cryptography;
using IoT.Application.Identity;

namespace IoT.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing.
///
/// Replaces the previous plain salted SHA-256. A single SHA-256 pass is far too cheap:
/// commodity hardware computes billions per second, so a leaked hash falls to brute force
/// almost immediately. PBKDF2 makes each guess deliberately expensive by repeating the
/// hash <see cref="Iterations"/> times, which costs a legitimate login a few milliseconds
/// and an attacker roughly 200,000x more work per guess.
///
/// This also guards the ESP32 device key, which goes through the same path.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    // OWASP's current floor for PBKDF2-HMAC-SHA256.
    private const int Iterations = 210_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        byte[] saltBytes;
        byte[] expected;

        try
        {
            saltBytes = Convert.FromBase64String(salt);
            expected = Convert.FromBase64String(hash);
        }
        catch (FormatException)
        {
            // Malformed stored credential: fail closed rather than throwing a 500.
            return false;
        }

        var actual = Derive(password, saltBytes);

        // Constant-time: a plain == on the strings would leak, through timing, how many
        // leading bytes matched, which lets an attacker recover a hash byte by byte.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
}
