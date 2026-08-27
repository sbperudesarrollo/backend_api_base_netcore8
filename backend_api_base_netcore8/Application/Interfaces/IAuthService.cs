using backend_api_base_netcore8.Application.DTOs.Login;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<LoginResponse?> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken);
}
