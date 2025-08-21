using System;

namespace HEMA.Models
{
    //save the fight? Yes, No, Continue Fight/
    public class ProtocolItem
    {
        public TimeSpan Time { get; set; }

        public int BlueScore { get; set; }
        public int RedScore { get; set; }
        public int BlueViolations{ get; set; }
        public int RedViolations{ get; set; }
        public int DoubleHits{ get; set; }

        public string PhraseDescription { get; set; }
        
        public Tempo? Tempo { get; set; }
        public ZoneType? RedHitZone { get; set; }
        public ZoneType? BlueHitZone { get; set; }
    }
}
