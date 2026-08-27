using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace backend_api_base_netcore8.Infrastructure.Repositories.Sql;

public class UserRepositorySql : IUserRepository, IUsersCrudRepository, IRoleRepository
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
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                SELECT TOP (1)
                    u.idUsuario,
                    u.usuario,
                    u.contrasenia,
                    u.nombres,
                    u.apellidos,
                    u.email,
                    u.telefono,
                    u.idRolUsuario,
                    r.Rol
                FROM prueba.tblUsuario u
                INNER JOIN prueba.tblRolUsuario r ON r.idRolUsuario = u.idRolUsuario
                WHERE u.email = @email;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 100) { Value = username });

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return MapLegacyUser(reader);
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
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                SELECT TOP (1)
                    u.idUsuario,
                    u.usuario,
                    u.contrasenia,
                    u.nombres,
                    u.apellidos,
                    u.email,
                    u.telefono,
                    u.idRolUsuario,
                    r.Rol
                FROM  prueba.tblUsuario u
                INNER JOIN prueba.tblRolUsuario r ON r.idRolUsuario = u.idRolUsuario
                WHERE u.idUsuario = @userId;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = userId });

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return MapLegacyUser(reader);
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
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                SELECT
                    u.idUsuario,
                    u.usuario,
                    u.contrasenia,
                    u.nombres,
                    u.apellidos,
                    u.email,
                    u.telefono,
                    u.idRolUsuario,
                    r.Rol
                FROM  prueba.tblUsuario u
                INNER JOIN prueba.tblRolUsuario r ON r.idRolUsuario = u.idRolUsuario
                ORDER BY u.idUsuario;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                users.Add(MapLegacyUser(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving legacy users.");
            throw;
        }

        return users;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                INSERT INTO  prueba.tblUsuario (usuario, contrasenia, nombres, apellidos, email, telefono, idRolUsuario)
                OUTPUT
                    INSERTED.idUsuario,
                    INSERTED.usuario,
                    INSERTED.contrasenia,
                    INSERTED.nombres,
                    INSERTED.apellidos,
                    INSERTED.email,
                    INSERTED.telefono,
                    INSERTED.idRolUsuario
                VALUES (@usuario, @contrasenia, @nombres, @apellidos, @email, @telefono, @idRolUsuario);
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            AddLegacyWriteParameters(command, user);

            User created;
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Legacy user insert did not return a row.");
                }

                created = MapLegacyUserWithoutRole(reader);
            }

            created.RoleName = await ResolveRoleNameAsync(connection, created.RoleId, cancellationToken).ConfigureAwait(false);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating legacy user {Email}", user.Email);
            throw;
        }
    }

    public async Task<User?> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                UPDATE  prueba.tblUsuario
                SET
                    usuario = @usuario,
                    contrasenia = @contrasenia,
                    nombres = @nombres,
                    apellidos = @apellidos,
                    email = @email,
                    telefono = @telefono,
                    idRolUsuario = @idRolUsuario
                OUTPUT
                    INSERTED.idUsuario,
                    INSERTED.usuario,
                    INSERTED.contrasenia,
                    INSERTED.nombres,
                    INSERTED.apellidos,
                    INSERTED.email,
                    INSERTED.telefono,
                    INSERTED.idRolUsuario
                WHERE idUsuario = @idUsuario;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new SqlParameter("@idUsuario", SqlDbType.Int) { Value = user.Id });
            AddLegacyWriteParameters(command, user);

            User? updated;
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                updated = MapLegacyUserWithoutRole(reader);
            }

            updated.RoleName = await ResolveRoleNameAsync(connection, updated.RoleId, cancellationToken).ConfigureAwait(false);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating legacy user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                "DELETE FROM  prueba.tblUsuario WHERE idUsuario = @idUsuario;",
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new SqlParameter("@idUsuario", SqlDbType.Int) { Value = id });
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting legacy user {UserId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<Role>> GetAllRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new List<Role>();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                SELECT idRolUsuario, Rol
                FROM prueba.tblRolUsuario
                ORDER BY idRolUsuario;
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
                    Id = GetInt32(reader, "idRolUsuario"),
                    Name = GetString(reader, "Rol")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving legacy roles.");
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
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                """
                UPDATE  prueba.tblUsuario
                SET contrasenia = @passwordHash
                WHERE idUsuario = @userId;
                """,
                connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            command.Parameters.Add(new SqlParameter("@passwordHash", SqlDbType.NVarChar, 150) { Value = passwordHash });
            command.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = userId });

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating legacy password for user id {UserId}", userId);
            throw;
        }
    }

    private static void AddLegacyWriteParameters(SqlCommand command, User user)
    {
        command.Parameters.Add(new SqlParameter("@usuario", SqlDbType.NVarChar, 60) { Value = user.Username });
        command.Parameters.Add(new SqlParameter("@contrasenia", SqlDbType.NVarChar, 150) { Value = user.Password });
        command.Parameters.Add(new SqlParameter("@nombres", SqlDbType.NVarChar, 100) { Value = user.FirstName });
        command.Parameters.Add(new SqlParameter("@apellidos", SqlDbType.NVarChar, 100) { Value = user.LastName });
        command.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 100) { Value = user.Email });
        command.Parameters.Add(new SqlParameter("@telefono", SqlDbType.Int)
        {
            Value = user.Phone.HasValue ? user.Phone.Value : DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@idRolUsuario", SqlDbType.Int) { Value = user.RoleId });
    }

    private static User MapLegacyUser(SqlDataReader reader)
    {
        return new User
        {
            Id = GetInt32(reader, "idUsuario"),
            Username = GetString(reader, "usuario"),
            Password = GetString(reader, "contrasenia"),
            FirstName = GetString(reader, "nombres"),
            LastName = GetString(reader, "apellidos"),
            Email = GetString(reader, "email"),
            Phone = GetNullableInt32(reader, "telefono"),
            RoleId = GetInt32(reader, "idRolUsuario"),
            RoleName = GetString(reader, "Rol")
        };
    }

    private static User MapLegacyUserWithoutRole(SqlDataReader reader)
    {
        return new User
        {
            Id = GetInt32(reader, "idUsuario"),
            Username = GetString(reader, "usuario"),
            Password = GetString(reader, "contrasenia"),
            FirstName = GetString(reader, "nombres"),
            LastName = GetString(reader, "apellidos"),
            Email = GetString(reader, "email"),
            Phone = GetNullableInt32(reader, "telefono"),
            RoleId = GetInt32(reader, "idRolUsuario"),
            RoleName = string.Empty
        };
    }

    private static string GetString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
    }

    private static int GetInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static int? GetNullableInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static async Task<string> ResolveRoleNameAsync(SqlConnection connection, int roleId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            "SELECT TOP (1) Rol FROM prueba.tblRolUsuario WHERE idRolUsuario = @idRolUsuario;",
            connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 0
        };

        command.Parameters.Add(new SqlParameter("@idRolUsuario", SqlDbType.Int) { Value = roleId });
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is null || result == DBNull.Value ? string.Empty : Convert.ToString(result) ?? string.Empty;
    }
}
