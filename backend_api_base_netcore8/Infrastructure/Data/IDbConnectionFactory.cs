using System.Data.Common;

namespace backend_api_base_netcore8.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DbConnection Create(string providerName);
}

