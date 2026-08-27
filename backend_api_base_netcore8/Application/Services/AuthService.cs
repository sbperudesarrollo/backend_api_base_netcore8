using System.Net.Http;
using System.Text.Json;
using backend_api_base_netcore8.Application.Configuration;
using backend_api_base_netcore8.Application.DTOs.Login;
using backend_api_base_netcore8.Application.DTOs.User;
using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace backend_api_base_netcore8.Application.Services;

public class AuthService : IAuthService
{
    private const int UnauthorizedRoleId = 22;
    private static readonly HttpClient GoogleHttpClient = new();

    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly GoogleAuthOptions _googleAuthOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IOptions<GoogleAuthOptions> googleAuthOptions,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _googleAuthOptions = googleAuthOptions.Value;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        ValidateLoginRequest(email, password);

        var user = await _userRepository.FindByUsernameAsync(email, cancellationToken);
        if (user is null)
        {
            LogFailedAttempt(email);
            return null;
        }

        EnsureAuthorizedRole(user);

        if (!VerifyPassword(password, user.Password))
        {
            LogFailedAttempt(email);
            return null;
        }

        var tokenResult = _tokenService.CreateToken(user);

        var userDto = MapToUserDto(user);

        return new LoginResponse
        {
            UserId = user.Id,
            Status = 1,
            Token = tokenResult.Token,
            ExpiresIn = tokenResult.ExpiresIn,
            User = userDto
        };
    }

    public async Task<LoginResponse?> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        ValidateGoogleLoginRequest(idToken);

        var tokenInfo = await ValidateGoogleTokenAsync(idToken, cancellationToken).ConfigureAwait(false);
        var user = await _userRepository.FindByUsernameAsync(tokenInfo.Email, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            _logger.LogWarning("Google login rejected for unknown email {Email}", tokenInfo.Email);
            return null;
        }

        EnsureAuthorizedRole(user);

        var tokenResult = _tokenService.CreateToken(user);
        return new LoginResponse
        {
            UserId = user.Id,
            Status = 1,
            Token = tokenResult.Token,
            ExpiresIn = tokenResult.ExpiresIn,
            User = MapToUserDto(user)
        };
    }

    private async Task<GoogleTokenInfo> ValidateGoogleTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
        {
            throw new InvalidOperationException("Configuracion de Google no disponible.");
        }

        var response = await GoogleHttpClient
            .GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException("Token de Google invalido.");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new UnauthorizedAccessException("Token de Google invalido.");
        }

        var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(payload);
        if (tokenInfo is null || string.IsNullOrWhiteSpace(tokenInfo.Email))
        {
            throw new UnauthorizedAccessException("Token de Google invalido.");
        }

        if (!string.Equals(tokenInfo.Audience, _googleAuthOptions.ClientId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Token de Google invalido.");
        }

        if (!tokenInfo.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Cuenta de Google no verificada.");
        }

        return tokenInfo;
    }

    private static bool VerifyPassword(string plainPassword, string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            return false;
        }

        if (LooksLikeBcrypt(storedPassword))
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, storedPassword);
        }

        return string.Equals(plainPassword?.Trim(), storedPassword?.Trim(), StringComparison.Ordinal);
    }

    private static bool LooksLikeBcrypt(string value) =>
        value.StartsWith("$2a$", StringComparison.Ordinal)
        || value.StartsWith("$2b$", StringComparison.Ordinal)
        || value.StartsWith("$2y$", StringComparison.Ordinal);

    private static void ValidateLoginRequest(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("El email es obligatorio.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("La contrasena es obligatoria.", nameof(password));
        }
    }

    private static void ValidateGoogleLoginRequest(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException("El token de Google es obligatorio.", nameof(idToken));
        }
    }

    private static void EnsureAuthorizedRole(User user)
    {
        if (user.RoleId == UnauthorizedRoleId)
        {
            throw new UnauthorizedAccessException("Usted no tiene asignado autorizacion para acceder. Comuniquese con el Administrador si desea acceder.");
        }
    }

    private static UserDto MapToUserDto(User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            Role = user.RoleName
        };

    private void LogFailedAttempt(string email) =>
        _logger.LogWarning("Failed login attempt for email {Email}", email);
}
