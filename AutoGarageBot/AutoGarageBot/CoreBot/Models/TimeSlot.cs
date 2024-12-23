using System.Collections.Generic;

namespace CoreBot.Models
{
    public class TimeSlot
    {
        public int TimeSlotId { get; set; }
        public string StartTime { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }
    }
}
