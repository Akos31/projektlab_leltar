using System.Collections.Generic;

namespace InventoryApp.Entities
{
    // Physical location of an asset. Deliberately NOT part of the Asset's
    // source data - the room is only known/recorded through scans.
    public class Room
    {
        public int Id { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public int InventoryZoneId { get; set; }
        public InventoryZone? InventoryZone { get; set; }

        public List<Scan> Scans { get; set; } = new();
    }
}
