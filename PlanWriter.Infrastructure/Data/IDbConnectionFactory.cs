using System.Data;

namespace PlanWriter.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}