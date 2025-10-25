using backend_api_base_netcore8.Application.DTOs.Login;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken);
}
