using backend_api_base_netcore8.Domain.Entities;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface ITokenService
{
    TokenResult CreateToken(User user);
}

public record TokenResult(string Token, int ExpiresIn);
