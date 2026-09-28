using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Identity;

/// <summary>
/// Login, refresh-token rotation and logout for the single owner account.
/// </summary>
public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokens;

    public AuthService(IAppDbContext db, IPasswordHasher passwordHasher, ITokenService tokens)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;

        var owner = await _db.OwnerAccounts
            .FirstOrDefaultAsync(o => o.Email.ToLower() == email.ToLower(), ct);

        // One message for "no such account" and "wrong password" alike, so the endpoint
        // cannot be used to enumerate which accounts exist.
        if (owner is null || !_passwordHasher.Verify(request.Password ?? string.Empty, owner.PasswordHash, owner.PasswordSalt))
            throw new UnauthorizedException("Invalid email or password.");

        owner.MarkLogin();

        return await IssueAndPersistAsync(owner, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException("Refresh token is required.");

        var hash = _tokens.HashRefreshToken(request.RefreshToken);

        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive(DateTime.UtcNow))
            throw new UnauthorizedException("Refresh token is invalid or has expired.");

        var owner = await _db.OwnerAccounts.FirstOrDefaultAsync(o => o.Id == stored.OwnerAccountId, ct)
            ?? throw new UnauthorizedException("Refresh token is invalid or has expired.");

        // Rotation: the presented token is spent. A stolen token therefore works at most
        // once, and the theft surfaces the next time the real client tries to refresh.
        stored.Revoke();

        return await IssueAndPersistAsync(owner, ct);
    }

    /// <summary>
    /// Revokes every refresh token for the account behind the presented token, so logging
    /// out on one device cannot be undone by a token kept elsewhere.
    /// Deliberately silent when the token is unknown: logout must never fail.
    /// </summary>
    public async Task LogoutAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var hash = _tokens.HashRefreshToken(request.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            return;

        var all = await _db.RefreshTokens
            .Where(t => t.OwnerAccountId == stored.OwnerAccountId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in all)
            token.Revoke();

        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> IssueAndPersistAsync(OwnerAccountEntity owner, CancellationToken ct)
    {
        var pair = _tokens.IssueTokens(owner.Id, owner.Email);

        _db.RefreshTokens.Add(new RefreshTokenEntity(owner.Id, pair.RefreshTokenHash, pair.RefreshTokenExpiresAtUtc));

        // Revoking the old token, marking the login and storing the new token all land in
        // one SaveChanges, so a failure cannot leave the account half-rotated.
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            pair.AccessToken,
            pair.AccessTokenExpiresAtUtc,
            pair.RefreshTokenRaw,
            pair.RefreshTokenExpiresAtUtc);
    }
}
