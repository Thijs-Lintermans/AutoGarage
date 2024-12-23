using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Models
{
    public class TimeSlot
    {
        public int TimeSlotId { get; set; }
        public string StartTime { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
    }
}
