using backend_api_base_netcore8.Domain.Entities;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface IUsersCrudRepository
{
    Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken);
    Task<User?> UpdateAsync(User user, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
