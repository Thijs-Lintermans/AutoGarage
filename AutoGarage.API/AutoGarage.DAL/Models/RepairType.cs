using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Models
{
    public class RepairType
    {
        public int RepairTypeId { get; set; }
        public string RepairName { get; set; }
        public string RepairDescription { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
    }
}
