using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace backend_api_base_netcore8.Infrastructure.Repositories.Oracle;

public class UserRepositoryOracle : IUserRepository, IUsersCrudRepository, IRoleRepository
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
        {
            return null;
        }

        try
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                SELECT
                    u.id,
                    u.role_id,
                    u.name,
                    u.first_name,
                    u.email,
                    u.password,
                    u.phone,
                    NVL(r.name, '') AS role_name
                FROM users u
                LEFT JOIN roles r ON r.id = u.role_id
                WHERE u.email = :email
                FETCH FIRST 1 ROWS ONLY
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            command.Parameters.Add(new OracleParameter("email", OracleDbType.Varchar2, username, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                SELECT
                    u.id,
                    u.role_id,
                    u.name,
                    u.first_name,
                    u.email,
                    u.password,
                    u.phone,
                    NVL(r.name, '') AS role_name
                FROM users u
                LEFT JOIN roles r ON r.id = u.role_id
                WHERE u.id = :userId
                FETCH FIRST 1 ROWS ONLY
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            command.Parameters.Add(new OracleParameter("userId", OracleDbType.Int32, userId, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                SELECT
                    u.id,
                    u.role_id,
                    u.name,
                    u.first_name,
                    u.email,
                    u.password,
                    u.phone,
                    NVL(r.name, '') AS role_name
                FROM users u
                LEFT JOIN roles r ON r.id = u.role_id
                ORDER BY u.id
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                INSERT INTO users (id, role_id, name, first_name, email, password, phone)
                VALUES (users_seq.NEXTVAL, :roleId, :username, :firstName, :email, :password, :phone)
                RETURNING id INTO :createdId
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            AddWriteParameters(command, user);

            var createdIdParameter = new OracleParameter("createdId", OracleDbType.Int32, ParameterDirection.Output);
            command.Parameters.Add(createdIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var createdId = Convert.ToInt32(createdIdParameter.Value);
            var createdUser = await GetByIdAsync(createdId, cancellationToken).ConfigureAwait(false);
            return createdUser ?? throw new InvalidOperationException("Oracle user insert did not return a row.");
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                UPDATE users
                SET
                    role_id = :roleId,
                    name = :username,
                    first_name = :firstName,
                    email = :email,
                    password = :password,
                    phone = :phone
                WHERE id = :userId
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            AddWriteParameters(command, user);
            command.Parameters.Add(new OracleParameter("userId", OracleDbType.Int32, user.Id, ParameterDirection.Input));

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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                "DELETE FROM users WHERE id = :userId",
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            command.Parameters.Add(new OracleParameter("userId", OracleDbType.Int32, id, ParameterDirection.Input));

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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                SELECT id, name
                FROM roles
                ORDER BY id
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                roles.Add(new Role
                {
                    Id = GetInt32(reader, "ID"),
                    Name = GetString(reader, "NAME")
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
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new OracleCommand(
                """
                UPDATE users
                SET password = :passwordHash
                WHERE id = :userId
                """,
                connection)
            {
                CommandType = CommandType.Text,
                BindByName = true
            };

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

    private static void AddWriteParameters(OracleCommand command, User user)
    {
        command.Parameters.Add(new OracleParameter("roleId", OracleDbType.Int32, user.RoleId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("username", OracleDbType.Varchar2, user.Username, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("firstName", OracleDbType.Varchar2, user.FirstName, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("email", OracleDbType.Varchar2, user.Email, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("password", OracleDbType.Varchar2, user.Password, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("phone", OracleDbType.Int32, user.Phone.HasValue ? user.Phone.Value : DBNull.Value, ParameterDirection.Input));
    }

    private static User MapUser(DbDataReader reader) =>
        new()
        {
            Id = GetInt32(reader, "ID"),
            RoleId = GetInt32(reader, "ROLE_ID"),
            Username = GetString(reader, "NAME"),
            FirstName = GetString(reader, "FIRST_NAME"),
            LastName = string.Empty,
            Email = GetString(reader, "EMAIL"),
            Password = GetString(reader, "PASSWORD"),
            Phone = GetNullableInt32(reader, "PHONE"),
            RoleName = GetString(reader, "ROLE_NAME")
        };

    private static string GetString(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
    }

    private static int GetInt32(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static int? GetNullableInt32(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }
}
