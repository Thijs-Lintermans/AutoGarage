using AutoGarage.DAL;
using AutoGarage.DAL.Models;
using Parkeerwachter.DAL;
using System;
using System.Linq;

namespace AutoGarage.DAL
{

    public class DBInitializer
    {
        public static void Initialize(AutoGarageContext context)
        {
            context.Database.EnsureCreated();

            // Look for any products.
            if (context.Customers.Any())
            {
                return;   // DB has been seeded
            }

            context.Customers.AddRange(
                new Customer
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Mail = "john.doe@example.com",
                    PhoneNumber = "123456789",
                    LicensePlate = "1-ABC-123"
                },
                new Customer
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Mail = "jane.smith@example.com",
                    PhoneNumber = "987654321",
                    LicensePlate = "2-DEF-456"
                }
            );
            context.SaveChanges();


            var timeSlots = new List<TimeSlot>();
            var startTime = new DateTime(2024, 10, 22, 9, 0, 0);
            for (var time = startTime; time.Hour < 17; time = time.AddMinutes(30))
            {
                timeSlots.Add(new TimeSlot
                {
                    StartTime = time.ToString("HH:mm")
                });
            }
            context.TimeSlots.AddRange(timeSlots);
            context.SaveChanges();

            context.RepairTypes.AddRange(
                new RepairType
                {
                    RepairName = "Engine Repair",
                    RepairDescription = "Fixing or replacing engine components"
                },
                new RepairType
                {
                    RepairName = "Tire Change",
                    RepairDescription = "Replacing worn-out tires"
                },
                new RepairType
                {
                    RepairName = "Large maintenance",
                    RepairDescription = "Large maintenance, including engine, transmission, and major components."
                },
                new RepairType
                {
                    RepairName = "Minor maintenance",
                    RepairDescription = "Minor maintenance, such as oil change, filter replacements, and checks."
                },
                new RepairType
                {
                    RepairName = "Oil Change",
                    RepairDescription = "Changing the engine oil to maintain proper engine performance."
                }
            );

            context.SaveChanges();

            context.Appointments.AddRange(
                new Appointment
                {
                    AppointmentDate = "2024-10-22", // Correct string format
                    TimeSlotId = context.TimeSlots.First().TimeSlotId,
                    RepairTypeId = context.RepairTypes.First().RepairTypeId,
                    CustomerId = context.Customers.First().CustomerId
                },
                new Appointment
                {
                    AppointmentDate = "2024-10-22", // Correct string format
                    TimeSlotId = context.TimeSlots.Skip(1).First().TimeSlotId,
                    RepairTypeId = context.RepairTypes.Skip(1).First().RepairTypeId,
                    CustomerId = context.Customers.Skip(1).First().CustomerId
                }

            );
            context.SaveChanges();

        }
    }
}
