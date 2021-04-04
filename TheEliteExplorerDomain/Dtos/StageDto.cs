namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Stage DTO.
    /// </summary>
    public class StageDto
    {
        /// <summary>
        /// Game identifier.
        /// </summary>
        public long GameId { get; set; }
        /// <summary>
        /// Stage code.
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Stage identifier.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Stage name.
        /// </summary>
        public string Name { get; set; }
    }
}
