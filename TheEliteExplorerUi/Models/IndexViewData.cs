using System.Collections.Generic;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerUi.Models
{
    public class IndexViewData
    {
        public IReadOnlyCollection<PlayerDto> Players { get; set; }
    }
}
