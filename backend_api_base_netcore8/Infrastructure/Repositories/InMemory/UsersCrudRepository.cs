using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using backend_api_base_netcore8.Infrastructure.Stores;

namespace backend_api_base_netcore8.Infrastructure.Repositories.InMemory;

public class UsersCrudRepository : IUsersCrudRepository
{
    private readonly AppDataStore _store;

    public UsersCrudRepository(AppDataStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<User> items = _store.Users.Values
            .OrderBy(x => x.Id)
            .ToArray();

        return Task.FromResult(items);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _store.Users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User user, CancellationToken cancellationToken)
    {
        user.Id = _store.GetNextUserId();
        _store.Users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User?> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        if (!_store.Users.ContainsKey(user.Id))
        {
            return Task.FromResult<User?>(null);
        }

        _store.Users[user.Id] = user;
        return Task.FromResult<User?>(user);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.Users.TryRemove(id, out _));
    }
}
