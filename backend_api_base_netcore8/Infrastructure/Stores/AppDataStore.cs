using System.Collections.Concurrent;
using System.Threading;
using backend_api_base_netcore8.Domain.Entities;

namespace backend_api_base_netcore8.Infrastructure.Stores;

public class AppDataStore
{
    private int _nextUserId = 2;

    public ConcurrentDictionary<int, User> Users { get; } = new();

    public AppDataStore()
    {
        Users.TryAdd(1, new User
        {
            Id = 1,
            Username = "admin",
            Password = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
            FirstName = "Administrador",
            LastName = "Sistema",
            Email = "admin@example.com",
            Phone = 999999999,
            RoleId = 1,
            RoleName = "Administrador"
        });

    
    }

    public int GetNextUserId() => Interlocked.Increment(ref _nextUserId);

}
