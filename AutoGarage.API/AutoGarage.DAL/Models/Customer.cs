using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mail {  get; set; }
        public string PhoneNumber { get; set; }
        public string LicensePlate { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
    }
}
