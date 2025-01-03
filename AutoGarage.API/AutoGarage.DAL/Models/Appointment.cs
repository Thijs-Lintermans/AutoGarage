﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public string AppointmentDate { get; set; }
        public int TimeSlotId { get; set; }
        public int RepairTypeId { get; set; }
        public int CustomerId { get; set; }

        public TimeSlot? TimeSlot { get; set; }
        public RepairType? RepairType { get; set; }
        public Customer? Customer { get; set; }


    }
}
