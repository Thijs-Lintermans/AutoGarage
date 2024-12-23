using System.Collections.Generic;

namespace CoreBot.Models
{
    public class RepairType
    {
        public int RepairTypeId { get; set; }
        public string RepairName { get; set; }
        public string RepairDescription { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }
    }
}
