using backend_api_base_netcore8.Application.DTOs.Login;
using backend_api_base_netcore8.Application.DTOs.User;
using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace backend_api_base_netcore8.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            LogFailedAttempt(username);
            return null;
        }

        if (!VerifyPassword(password, user.Password))
        {
            LogFailedAttempt(username);
            return null;
        }

        var tokenResult = _tokenService.CreateToken(user);

        var userDto = MapToUserDto(user);

        return new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresIn = tokenResult.ExpiresIn,
            //User = userDto
        };
    }

    private static bool VerifyPassword(string plainPassword, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);

    private static UserDto MapToUserDto(User user) =>
        new()
        {
            Id = user.Id,
            RoleId = user.RoleId,
            Name = user.Name,
            FirstName = user.FirstName,
            Email = user.Email,
            DegreeId = user.DegreeId,
            Phone = user.Phone,
            Cip = user.Cip
        };

    private void LogFailedAttempt(string email) =>
        _logger.LogWarning("Failed login attempt for email {Email}", email);
}
