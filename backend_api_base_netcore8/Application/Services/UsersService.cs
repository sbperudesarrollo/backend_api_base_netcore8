using backend_api_base_netcore8.Application.DTOs.Common;
using backend_api_base_netcore8.Application.DTOs.Roles;
using backend_api_base_netcore8.Application.DTOs.Users;
using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;

namespace backend_api_base_netcore8.Application.Services;

public class UsersService : IUsersService
{
    private readonly IUsersCrudRepository _usersRepository;
    private readonly IRoleRepository _roleRepository;

    public UsersService(IUsersCrudRepository usersRepository, IRoleRepository roleRepository)
    {
        _usersRepository = usersRepository;
        _roleRepository = roleRepository;
    }

    public async Task<CollectionResponse<UserResponse>> GetAllAsync(int page, int perPage, CancellationToken cancellationToken)
    {
        if (page <= 0)
        {
            throw new ArgumentException("Page debe ser mayor a cero.", nameof(page));
        }

        if (perPage <= 0)
        {
            throw new ArgumentException("PerPage debe ser mayor a cero.", nameof(perPage));
        }

        var allItems = (await _usersRepository.GetAllAsync(cancellationToken))
            .OrderBy(x => x.Id)
            .Select(Map)
            .ToArray();

        var total = allItems.Length;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)perPage);
        var items = allItems
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToArray();

        return new CollectionResponse<UserResponse>
        {
            Success = true,
            Data = items,
            Meta = new CollectionMeta
            {
                Page = page,
                PerPage = perPage,
                Total = total,
                TotalPages = totalPages
            }
        };
    }

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Id invalido.", nameof(id));
        }

        var user = await _usersRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllRolesAsync(cancellationToken);
        return roles
            .OrderBy(x => x.Id)
            .Select(x => new RoleResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToArray();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        var user = new User
        {
            RoleId = request.RoleId,
            Username = request.Username,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            RoleName = string.Empty
        };

        var createdUser = await _usersRepository.CreateAsync(user, cancellationToken);
        return Map(createdUser);
    }

    public async Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Id invalido para actualizacion.", nameof(id));
        }

        ValidateUpdateRequest(request);

        var user = await _usersRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Username = request.Username;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.RoleId = request.RoleId;
        user.Phone = request.Phone;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        var updatedUser = await _usersRepository.UpdateAsync(user, cancellationToken);
        return updatedUser is null ? null : Map(updatedUser);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Id invalido.", nameof(id));
        }

        return _usersRepository.DeleteAsync(id, cancellationToken);
    }

    private static void ValidateCreateRequest(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateCommonRequest(request.Username, request.Email, request.RoleId);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("La contrasena es obligatoria para crear el usuario.", nameof(request.Password));
        }
    }

    private static void ValidateUpdateRequest(UpdateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateCommonRequest(request.Username, request.Email, request.RoleId);
    }

    private static void ValidateCommonRequest(string username, string email, int roleId)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("El email es obligatorio.", nameof(email));
        }

        if (roleId <= 0)
        {
            throw new ArgumentException("Debe asignar un rol valido.", nameof(roleId));
        }
    }

    private static UserResponse Map(User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = user.RoleId,
            Role = user.RoleName,
            Phone = user.Phone
        };
}
