using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace backend_api_base_netcore8.Infrastructure.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _cfg;
    public DbConnectionFactory(IConfiguration cfg) => _cfg = cfg;

    public DbConnection Create(string providerName)
    {
        var cs = _cfg.GetConnectionString(providerName)
                 ?? throw new InvalidOperationException($"CS '{providerName}' no encontrada.");

        return providerName switch
        {
            "MySql" => new MySqlConnection(cs),
            "SqlServer" => new SqlConnection(cs),
            "PostgreSql" => new NpgsqlConnection(cs),
            "Oracle" => new OracleConnection(cs),
            _ => throw new NotSupportedException($"Proveedor '{providerName}' no soportado.")
        };
    }
}