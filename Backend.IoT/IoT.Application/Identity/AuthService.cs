using FluentValidation;
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
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokens,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshRequest> refreshValidator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim();

        var owner = await _db.OwnerAccounts
            .FirstOrDefaultAsync(o => o.Email.ToLower() == email.ToLower(), ct);

        // One message for "no such account" and "wrong password" alike, so the endpoint
        // cannot be used to enumerate which accounts exist.
        if (owner is null || !_passwordHasher.Verify(request.Password, owner.PasswordHash, owner.PasswordSalt))
            throw new UnauthorizedException("Invalid email or password.");

        owner.LastLoginAtUtc = DateTime.UtcNow;

        return await IssueAndPersistAsync(owner, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        await _refreshValidator.ValidateAndThrowAsync(request, ct);

        var hash = _tokens.HashRefreshToken(request.RefreshToken);

        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive(DateTime.UtcNow))
            throw new UnauthorizedException("Refresh token is invalid or has expired.");

        var owner = await _db.OwnerAccounts.FirstOrDefaultAsync(o => o.Id == stored.OwnerAccountId, ct)
            ?? throw new UnauthorizedException("Refresh token is invalid or has expired.");

        // Rotation: the presented token is spent. A stolen token therefore works at most
        // once, and the theft surfaces the next time the real client tries to refresh.
        Revoke(stored);

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
            Revoke(token);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Idempotent: revoking an already-revoked token must not move its timestamp.</summary>
    private static void Revoke(RefreshToken token)
    {
        if (token.IsRevoked)
            return;

        token.IsRevoked = true;
        token.RevokedAtUtc = DateTime.UtcNow;
    }

    private async Task<AuthResponse> IssueAndPersistAsync(OwnerAccount owner, CancellationToken ct)
    {
        var pair = _tokens.IssueTokens(owner.Id, owner.Email);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = owner.Id,
            TokenHash = pair.RefreshTokenHash,
            ExpiresAtUtc = pair.RefreshTokenExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            IsRevoked = false
        });

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
