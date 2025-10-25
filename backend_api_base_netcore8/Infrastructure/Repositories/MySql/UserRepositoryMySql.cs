using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using System.Data;
using MySqlConnector;

namespace backend_api_base_netcore8.Infrastructure.Repositories.MySql;

public class UserRepositoryMySql : IUserRepository //habilitar si se utilizara la base de datos MySql
{
    private readonly string _connectionString;
    private readonly ILogger<UserRepositoryMySql> _logger;

    public UserRepositoryMySql(IConfiguration configuration, ILogger<UserRepositoryMySql> logger)
    {
        _connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Connection string 'MySql' was not found.");
        _logger = logger;
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(@"
                SELECT
                    id,
                    role_id,
                    name,
                    first_name,
                    email,
                    password,
                    degree_id,
                    remember_token,
                    phone,
                    cip
                FROM users
                WHERE name = @username
                LIMIT 1;
            ", connection)
            { CommandType = CommandType.Text, CommandTimeout = 0 };

            command.Parameters.Add(new MySqlParameter("@username", MySqlDbType.VarChar) { Value = username });

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return MapUser(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with username {Username}", username);
            throw;
        }

        return null;
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(@"
                SELECT
                    id,
                    role_id,
                    name,
                    first_name,
                    email,
                    password,
                    degree_id,
                    remember_token,
                    phone,
                    cip
                FROM users
                WHERE id = @userId
                LIMIT 1;
            ", connection)
            { CommandType = CommandType.Text, CommandTimeout = 0 };

            command.Parameters.Add(new MySqlParameter("@userId", MySqlDbType.Int32) { Value = userId });

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return MapUser(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with id {UserId}", userId);
            throw;
        }

        return null;
    }

    public async Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(@"
                UPDATE users
                SET password = @passwordHash
                WHERE id = @userId;
            ", connection)
            { CommandType = CommandType.Text, CommandTimeout = 0 };

            command.Parameters.Add(new MySqlParameter("@passwordHash", MySqlDbType.VarChar) { Value = passwordHash });
            command.Parameters.Add(new MySqlParameter("@userId", MySqlDbType.Int32) { Value = userId });

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password hash for user id {UserId}", userId);
            throw;
        }
    }

    private static User MapUser(MySqlDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal("id");
        var roleIdOrdinal = reader.GetOrdinal("role_id");
        var nameOrdinal = reader.GetOrdinal("name");
        var firstNameOrdinal = reader.GetOrdinal("first_name");
        var emailOrdinal = reader.GetOrdinal("email");
        var passwordOrdinal = reader.GetOrdinal("password");
        var degreeIdOrdinal = reader.GetOrdinal("degree_id");
        var rememberTokenOrdinal = reader.GetOrdinal("remember_token");
        var phoneOrdinal = reader.GetOrdinal("phone");
        var cipOrdinal = reader.GetOrdinal("cip");

        return new User
        {
            Id = reader.GetInt32(idOrdinal),
            RoleId = reader.GetInt32(roleIdOrdinal),
            Name = GetRequiredString(reader, nameOrdinal),
            FirstName = GetRequiredString(reader, firstNameOrdinal),
            Email = GetRequiredString(reader, emailOrdinal),
            Password = GetRequiredString(reader, passwordOrdinal),
            DegreeId = GetNullableInt32(reader, degreeIdOrdinal),
            RememberToken = GetNullableString(reader, rememberTokenOrdinal),
            Phone = GetNullableInt64(reader, phoneOrdinal),
            Cip = GetNullableInt64(reader, cipOrdinal)
        };
    }

    private static string GetRequiredString(MySqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

    private static string? GetNullableString(MySqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? GetNullableInt32(MySqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static long? GetNullableInt64(MySqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal)); // maneja BIGINT/DECIMAL
}
