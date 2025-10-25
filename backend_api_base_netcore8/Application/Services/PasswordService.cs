using System.Security.Cryptography;
using backend_api_base_netcore8.Application.DTOs;
using backend_api_base_netcore8.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend_api_base_netcore8.Application.Services;

public class PasswordService : IPasswordService
{
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Specials = "!@#$%^&*()-_=+[]{}|;:,.<>?";

    private static readonly char[] AllCharacters = (Upper + Lower + Digits + Specials).ToCharArray();

    private readonly IUserRepository _userRepository;
    private readonly ILogger<PasswordService> _logger;

    public PasswordService(IUserRepository userRepository, ILogger<PasswordService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<PasswordGenerationResponse?> GenerateAndUpdatePasswordAsync(
        GeneratePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Attempted to generate password for non-existent user {UserId}", request.UserId);
            return null;
        }

        var generatedPassword = GenerateSecurePassword(request.Length);
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

        var updated = await _userRepository.UpdatePasswordHashAsync(user.Id, hashedPassword, cancellationToken);
        if (!updated)
        {
            _logger.LogError("Failed to persist password change for user {UserId}", user.Id);
            return null;
        }

        _logger.LogInformation("Password regenerated for user {UserId}", user.Id);

        return new PasswordGenerationResponse
        {
            UserId = user.Id,
            Password = generatedPassword
        };
    }

    private static string GenerateSecurePassword(int length)
    {
        Span<char> password = stackalloc char[length];
        var categories = new[]
        {
            Upper.ToCharArray(),
            Lower.ToCharArray(),
            Digits.ToCharArray(),
            Specials.ToCharArray()
        };

        // Ensure at least one char from each category when possible
        var categoryCount = Math.Min(categories.Length, length);
        var position = 0;
        for (; position < categoryCount; position++)
        {
            password[position] = GetRandomChar(categories[position]);
        }

        for (; position < length; position++)
        {
            password[position] = GetRandomChar(AllCharacters);
        }

        Shuffle(password);
        return new string(password);
    }

    private static char GetRandomChar(ReadOnlySpan<char> source)
    {
        var index = RandomNumberGenerator.GetInt32(source.Length);
        return source[index];
    }

    private static void Shuffle(Span<char> buffer)
    {
        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }
    }
}
