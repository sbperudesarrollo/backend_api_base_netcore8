using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;

using System.Data;
using System.Data.Common;                    // <-- añadido
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace backend_api_base_netcore8.Infrastructure.Repositories.Oracle;

public class UserRepositoryOracle // : IUserRepository  // habilita si usarás Oracle
{
    private readonly string _connectionString;
    private readonly ILogger<UserRepositoryOracle> _logger;

    public UserRepositoryOracle(IConfiguration configuration, ILogger<UserRepositoryOracle> logger)
    {
        _connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("Connection string 'Oracle' was not found.");
        _logger = logger;
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        try
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(@"
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
                WHERE name = :username
                FETCH FIRST 1 ROWS ONLY
            ", connection)
            { CommandType = CommandType.Text, BindByName = true };

            command.Parameters.Add(new OracleParameter("username", OracleDbType.Varchar2, username, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return MapUser(reader);              // ahora acepta DbDataReader
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(@"
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
                WHERE id = :userId
                FETCH FIRST 1 ROWS ONLY
            ", connection)
            { CommandType = CommandType.Text, BindByName = true };

            command.Parameters.Add(new OracleParameter("userId", OracleDbType.Int32, userId, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(@"
                UPDATE users
                SET password = :passwordHash
                WHERE id = :userId
            ", connection)
            { CommandType = CommandType.Text, BindByName = true };

            command.Parameters.Add(new OracleParameter("passwordHash", OracleDbType.Varchar2, passwordHash, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter("userId", OracleDbType.Int32, userId, ParameterDirection.Input));

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password hash for user id {UserId}", userId);
            throw;
        }
    }

    // -------------------------
    // Mapping agnóstico (DbDataReader)
    // -------------------------
    private static User MapUser(DbDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal("ID");
        var roleIdOrdinal = reader.GetOrdinal("ROLE_ID");
        var nameOrdinal = reader.GetOrdinal("NAME");
        var firstNameOrdinal = reader.GetOrdinal("FIRST_NAME");
        var emailOrdinal = reader.GetOrdinal("EMAIL");
        var passwordOrdinal = reader.GetOrdinal("PASSWORD");
        var degreeIdOrdinal = reader.GetOrdinal("DEGREE_ID");
        var rememberTokenOrdinal = reader.GetOrdinal("REMEMBER_TOKEN");
        var phoneOrdinal = reader.GetOrdinal("PHONE");
        var cipOrdinal = reader.GetOrdinal("CIP");

        return new User
        {
            Id = GetInt32(reader, idOrdinal),
            RoleId = GetInt32(reader, roleIdOrdinal),
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

    private static string GetRequiredString(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

    private static string? GetNullableString(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int GetInt32(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));   // seguro para NUMBER(*)

    private static int? GetNullableInt32(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));

    private static long? GetNullableInt64(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
}
