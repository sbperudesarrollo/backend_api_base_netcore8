using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend_api_base_netcore8.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public TokenResult CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresMinutes = _jwtOptions.ExpiresMinutes > 0 ? _jwtOptions.ExpiresMinutes : 60;
        var expires = now.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            //new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            //new(JwtRegisteredClaimNames.Email, user.Email),
            //new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            //new(JwtRegisteredClaimNames.UniqueName, user.Name),
            new("user-id", user.Id.ToString() ?? string.Empty),
            new("email", user.Email.ToString() ?? string.Empty),
            new("firstName", user.FirstName.ToString() ?? string.Empty),
            new("username", user.Name.ToString() ?? string.Empty),
            new("cip", user.Cip?.ToString() ?? string.Empty),
            new("role_id", user.RoleId.ToString()),
            new("degree_id", user.DegreeId?.ToString() ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, ToUnixEpoch(now).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, ToUnixEpoch(now).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Exp, ToUnixEpoch(expires).ToString(), ClaimValueTypes.Integer64)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(jwt);

        var expiresInSeconds = (int)Math.Round((expires - now).TotalSeconds);

        return new TokenResult(token, expiresInSeconds);
    }

    private static long ToUnixEpoch(DateTime dateTime) =>
        (long)Math.Round((dateTime - DateTime.UnixEpoch).TotalSeconds);
}
