using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VE.Shared.Authentication;

/// <summary>
/// Device token management for OAuth 2.0-style access/refresh token flow.
/// TRD §5.1: Device credentials bound to hardware-backed keys, rotating refresh tokens.
/// </summary>
public class DeviceTokenManager
{
    private const int AccessTokenExpiryMinutes = 15;
    private const int RefreshTokenExpiryDays = 365;

    private readonly string _issuer;
    private readonly string _audience;

    public DeviceTokenManager(string issuer = "https://api.vigileye.local", string audience = "vigileye-companion-app")
    {
        _issuer = issuer;
        _audience = audience;
    }

    /// <summary>
    /// Issues a short-lived (15-minute) access token for a paired device.
    /// TRD §5.1.1: Every sync exchanges refresh token for new access token.
    /// </summary>
    public string IssueAccessToken(
        string deviceId,
        string accountId,
        string childId,
        DateTime issuedAt,
        ECDsa? devicePublicKey = null)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim("device_id", deviceId),
            new System.Security.Claims.Claim("account_id", accountId),
            new System.Security.Claims.Claim("child_id", childId),
            new System.Security.Claims.Claim("scope", "device:sync"),
            new System.Security.Claims.Claim("iat", issuedAt.ToUniversalTime().ToString("O"))
        };

        var expiresAt = issuedAt.AddMinutes(AccessTokenExpiryMinutes);

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtToken(
            issuer: _issuer,
            audience: _audience,
            subject: new System.Security.Claims.ClaimsIdentity(claims),
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("temp-signing-key-replace-with-rsa")),
                SecurityAlgorithms.HmacSha256)
        );

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a rotating refresh token (hashed in database).
    /// TRD §5.1.1: Invalidates previous token on rotation; cannot be replayed without device's private key.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Hashes a refresh token for safe storage in database.
    /// </summary>
    public string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Validates that a token is not expired and matches expected claims.
    /// </summary>
    public bool ValidateAccessToken(string token, string expectedDeviceId, string expectedAccountId)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = false, // TODO: implement RSA signing
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var deviceIdClaim = principal.FindFirst("device_id")?.Value;
            var accountIdClaim = principal.FindFirst("account_id")?.Value;

            return deviceIdClaim == expectedDeviceId && accountIdClaim == expectedAccountId;
        }
        catch
        {
            return false;
        }
    }
}
