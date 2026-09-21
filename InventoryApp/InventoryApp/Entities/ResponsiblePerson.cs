using System.Collections.Generic;

namespace InventoryApp.Entities
{
    public class ResponsiblePerson
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Asset> Assets { get; set; } = new();
    }
}
