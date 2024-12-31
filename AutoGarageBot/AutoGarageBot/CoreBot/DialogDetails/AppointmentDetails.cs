using CoreBot.Models;
using System;

namespace CoreBot.DialogDetails
{
    public class AppointmentDetails
    {
        public string AppointmentDate { get; set; }
        public int RepairTypeId { get; set; }
        public int TimeSlotId { get; set; }
        public int CustomerId { get; set; }

        public TimeSlot? TimeSlot { get; set; }
        public RepairType? RepairType { get; set; }
        public CustomerDetails Customer { get; set; }
    }
}
