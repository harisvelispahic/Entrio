using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IoT.Application.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IoT.Infrastructure.Identity;

/// <summary>
/// Mints access tokens (short-lived JWTs) and refresh tokens (long-lived random secrets).
///
/// The split exists so the access token can be short-lived without forcing the user to log
/// in every few minutes: it is presented on every request and cannot be revoked once
/// issued, so a short lifetime bounds the damage of a leak. The refresh token is presented
/// only to /api/auth/refresh, is stored server-side, and can therefore be revoked at any
/// time — which is what makes logout meaningful.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    private int AccessTokenMinutes => _config.GetValue("Jwt:AccessTokenMinutes", 15);
    private int RefreshTokenDays => _config.GetValue("Jwt:RefreshTokenDays", 7);

    public TokenPair IssueTokens(Guid ownerId, string email)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("No JWT signing key. Set JWT_KEY in the repo-root .env file.");

        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, ownerId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            // Unique per token, so individual tokens are identifiable in logs.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("owner", "true")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // 64 bytes of CSPRNG output. Unlike the JWT this carries no structure and means
        // nothing on its own — it is just a lookup key for the stored hash.
        var refreshRaw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        return new TokenPair(
            accessToken,
            accessExpires,
            refreshRaw,
            HashRefreshToken(refreshRaw),
            now.AddDays(RefreshTokenDays));
    }

    public string HashRefreshToken(string rawToken)
    {
        // SHA-256 without stretching is right here, unlike for passwords: the input is 64
        // random bytes, so there is no guessable keyspace for an attacker to search.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Base64UrlEncoder.Encode(bytes);
    }
}
