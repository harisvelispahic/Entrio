using IoT.Application.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Exchanges email + password for an access/refresh token pair.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>
    /// Exchanges a valid refresh token for a new pair, revoking the presented one.
    /// Anonymous by design: the caller's access token has expired, so the refresh token
    /// is what authenticates this request.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
        => Ok(await _auth.RefreshAsync(request, ct));

    /// <summary>
    /// Revokes every refresh token for the account. Always returns 204, including for an
    /// unknown token — a client trying to log out should never be told "no".
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        await _auth.LogoutAsync(request, ct);
        return NoContent();
    }
}
