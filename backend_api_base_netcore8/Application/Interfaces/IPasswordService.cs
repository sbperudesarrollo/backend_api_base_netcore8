using backend_api_base_netcore8.Application.DTOs;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface IPasswordService
{
    Task<PasswordGenerationResponse?> GenerateAndUpdatePasswordAsync(GeneratePasswordRequest request, CancellationToken cancellationToken);
}
