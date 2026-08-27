using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace backend_api_base_netcore8.Infrastructure.Repositories.Postgres;

public class UserRepositoryPostgres : IUserRepository, IUsersCrudRepository, IRoleRepository
{
    private readonly string _connectionString;
    private readonly ILogger<UserRepositoryPostgres> _logger;

    public UserRepositoryPostgres(IConfiguration configuration, ILogger<UserRepositoryPostgres> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("Connection string 'PostgreSql' was not found.");
        _logger = logger;
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                SELECT
                    u.id,
                    u.role_id,
                    u.name,
                    u.first_name,
                    u.email,
                    u.password,
                    u.phone,
                    COALESCE(r.name, '') AS role_name
                FROM public.users u
                LEFT JOIN public.roles r ON r.id = u.role_id
                WHERE u.email = @email
                LIMIT 1;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new NpgsqlParameter("@email", NpgsqlDbType.Varchar) { Value = username });

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return MapUser(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", username);
            throw;
        }

        return null;
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                SELECT
                    u.id,
                    u.role_id,
                    u.name,
                    u.first_name,
                    u.email,
                    u.password,
                    u.phone,
                    COALESCE(r.name, '') AS role_name
                FROM public.users u
                LEFT JOIN public.roles r ON r.id = u.role_id
                WHERE u.id = @userId
                LIMIT 1;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new NpgsqlParameter("@userId", NpgsqlDbType.Integer) { Value = userId });

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return MapUser(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with id {UserId}", userId);
            throw;
        }

        return null;
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = new List<User>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                SELECT
                    u.id,
                    u.role_id,
                    u.name,
                    u.first_name,
                    u.email,
                    u.password,
                    u.phone,
                    COALESCE(r.name, '') AS role_name
                FROM public.users u
                LEFT JOIN public.roles r ON r.id = u.role_id
                ORDER BY u.id;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                users.Add(MapUser(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users.");
            throw;
        }

        return users;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                INSERT INTO public.users (role_id, name, first_name, email, password, phone)
                VALUES (@roleId, @username, @firstName, @email, @password, @phone)
                RETURNING id;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            AddWriteParameters(command, user);

            var createdId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            var createdUser = await GetByIdAsync(createdId, cancellationToken).ConfigureAwait(false);
            return createdUser ?? throw new InvalidOperationException("PostgreSql user insert did not return a row.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", user.Email);
            throw;
        }
    }

    public async Task<User?> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                UPDATE public.users
                SET
                    role_id = @roleId,
                    name = @username,
                    first_name = @firstName,
                    email = @email,
                    password = @password,
                    phone = @phone
                WHERE id = @userId;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new NpgsqlParameter("@userId", NpgsqlDbType.Integer) { Value = user.Id });
            AddWriteParameters(command, user);

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0
                ? await GetByIdAsync(user.Id, cancellationToken).ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                "DELETE FROM public.users WHERE id = @userId;",
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new NpgsqlParameter("@userId", NpgsqlDbType.Integer) { Value = id });

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<Role>> GetAllRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new List<Role>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                SELECT id, name
                FROM public.roles
                ORDER BY id;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                roles.Add(new Role
                {
                    Id = GetInt32(reader, "id"),
                    Name = GetString(reader, "name")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles.");
            throw;
        }

        return roles;
    }

    public async Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(
                """
                UPDATE public.users
                SET password = @passwordHash
                WHERE id = @userId;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new NpgsqlParameter("@passwordHash", NpgsqlDbType.Varchar) { Value = passwordHash });
            command.Parameters.Add(new NpgsqlParameter("@userId", NpgsqlDbType.Integer) { Value = userId });

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password hash for user id {UserId}", userId);
            throw;
        }
    }

    private static void AddWriteParameters(NpgsqlCommand command, User user)
    {
        command.Parameters.Add(new NpgsqlParameter("@roleId", NpgsqlDbType.Integer) { Value = user.RoleId });
        command.Parameters.Add(new NpgsqlParameter("@username", NpgsqlDbType.Varchar) { Value = user.Username });
        command.Parameters.Add(new NpgsqlParameter("@firstName", NpgsqlDbType.Varchar) { Value = user.FirstName });
        command.Parameters.Add(new NpgsqlParameter("@email", NpgsqlDbType.Varchar) { Value = user.Email });
        command.Parameters.Add(new NpgsqlParameter("@password", NpgsqlDbType.Varchar) { Value = user.Password });
        command.Parameters.Add(new NpgsqlParameter("@phone", NpgsqlDbType.Integer)
        {
            Value = user.Phone.HasValue ? user.Phone.Value : DBNull.Value
        });
    }

    private static User MapUser(NpgsqlDataReader reader) =>
        new()
        {
            Id = GetInt32(reader, "id"),
            RoleId = GetInt32(reader, "role_id"),
            Username = GetString(reader, "name"),
            FirstName = GetString(reader, "first_name"),
            LastName = string.Empty,
            Email = GetString(reader, "email"),
            Password = GetString(reader, "password"),
            Phone = GetNullableInt32(reader, "phone"),
            RoleName = GetString(reader, "role_name")
        };

    private static string GetString(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static int GetInt32(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static int? GetNullableInt32(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }
}
