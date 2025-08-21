using System.Collections.Generic;

namespace HEMA.Models
{
    public class Protocol
    {
        public string RedName { get; set; } = "Red";
        public string BlueName { get; set; } = "Blue";
        public List<ProtocolItem> Exchanges { get; set; } = new List<ProtocolItem>();
    }
}
