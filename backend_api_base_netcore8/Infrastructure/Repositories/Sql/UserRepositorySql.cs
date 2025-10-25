using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Data;
namespace backend_api_base_netcore8.Infrastructure.Repositories.Sql;
public class UserRepositorySql //: IUserRepository //habilitar si se utilizara la base de datos SqlServer
{
    private readonly string _connectionString;
    private readonly ILogger<UserRepositorySql> _logger;
    public UserRepositorySql(IConfiguration configuration, ILogger<UserRepositorySql> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' was not found.");
        _logger = logger;
    }
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username)) { return null; }
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = new SqlCommand(""" SELECT TOP (1) id, role_id, name, first_name, email, password, degree_id, remember_token, phone, cip FROM dbo.users WHERE name = @username; """,
                connection)
            { CommandType = CommandType.Text, CommandTimeout = 0 }; command.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 256) { Value = username });
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) { return MapUser(reader); }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving user with username {Username}", username); throw; }
        return null;
    }
    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = new SqlCommand(""" SELECT TOP (1) id, role_id, name, first_name, email, password, degree_id, remember_token, phone, cip FROM dbo.users WHERE id = @userId; """,
                connection)
            { CommandType = CommandType.Text, CommandTimeout = 0 };
            command.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = userId });
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false); if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) { return MapUser(reader); }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving user with id {UserId}", userId); throw; }
        return null;
    }
    public async Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) { return false; }
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = new SqlCommand(""" UPDATE dbo.users SET password = @passwordHash WHERE id = @userId; """, connection)
            { CommandType = CommandType.Text, CommandTimeout = 0 };
            command.Parameters.Add(new SqlParameter("@passwordHash", SqlDbType.NVarChar, 255) { Value = passwordHash });
            command.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = userId });
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false); return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password hash for user id {UserId}", userId); throw;
        }
    }
    private static User MapUser(SqlDataReader reader)
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
    private static string GetRequiredString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    private static string? GetNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static int? GetNullableInt32(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static long? GetNullableInt64(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
}