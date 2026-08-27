using backend_api_base_netcore8.Application.DTOs.Common;
using backend_api_base_netcore8.Application.DTOs.Roles;
using backend_api_base_netcore8.Application.DTOs.Users;

namespace backend_api_base_netcore8.Application.Interfaces;

public interface IUsersService
{
    Task<CollectionResponse<UserResponse>> GetAllAsync(int page, int perPage, CancellationToken cancellationToken);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
