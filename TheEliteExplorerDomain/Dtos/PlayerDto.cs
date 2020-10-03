namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a row in the "player" table.
    /// </summary>
    public class PlayerDto
    {
        /// <summary>
        /// "id" column value.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// "url_name" column value.
        /// </summary>
        public string UrlName { get; set; }
        /// <summary>
        /// "real_name" column value.
        /// </summary>
        public string RealName { get; set; }
        /// <summary>
        /// "surname" column value.
        /// </summary>
        public string SurName { get; set; }
        /// <summary>
        /// "control_style" column value.
        /// </summary>
        /// <remarks>Nullable.</remarks>
        public string ControlStyle { get; set; }
        /// <summary>
        /// "color" column value.
        /// </summary>
        public string Color { get; set; }
    }
}
