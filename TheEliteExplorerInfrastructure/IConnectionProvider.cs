using System.Data;

namespace TheEliteExplorerInfrastructure
{
    public interface IConnectionProvider
    {
        IDbConnection TheEliteConnection { get; }
    }
}
