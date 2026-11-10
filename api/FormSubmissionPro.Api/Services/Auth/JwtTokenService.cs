using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FormSubmissionPro.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace FormSubmissionPro.Api.Services.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "FormSubmissionPro";
    public string Audience { get; set; } = "FormSubmissionPro";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 24 * 7;
}

public interface IJwtTokenService
{
    string CreateToken(User user);
}

/// <summary>Issues signed JWTs carrying the user id, org id, and email.</summary>
public class JwtTokenService : IJwtTokenService
{
    public const string OrgClaim = "org";

    private readonly JwtOptions _opt;
    public JwtTokenService(JwtOptions opt) => _opt = opt;

    public string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(OrgClaim, user.OrganizationId.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_opt.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
