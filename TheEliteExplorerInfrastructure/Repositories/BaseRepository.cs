namespace TheEliteExplorerInfrastructure.Repositories
{
    /// <summary>
    /// Base repository.
    /// </summary>
    public abstract class BaseRepository
    {
        /// <summary>
        /// Gets full stored procedure name.
        /// </summary>
        /// <param name="baseName">Base name.</param>
        /// <returns>Full name.</returns>
        protected string ToPsName(string baseName)
        {
            return $"[dbo].[{baseName}]";
        }
    }
}
