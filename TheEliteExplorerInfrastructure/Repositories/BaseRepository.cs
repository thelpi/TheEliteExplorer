namespace TheEliteExplorerInfrastructure.Repositories
{
    public abstract class BaseRepository
    {
        protected string ToPsName(string baseName)
        {
            return $"[dbo].[{baseName}]";
        }
    }
}
