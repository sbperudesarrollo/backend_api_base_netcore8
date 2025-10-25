using backend_api_base_netcore8.Domain.Entities;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken);
}
