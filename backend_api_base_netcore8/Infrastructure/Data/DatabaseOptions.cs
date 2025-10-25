namespace backend_api_base_netcore8.Infrastructure.Data;

public enum DatabaseProvider
{
    MySql,
    SqlServer,
    PostgreSql,
    Oracle
}

public class DatabaseOptions
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MySql;
    public string ConnectionString { get; set; } = string.Empty;
}
