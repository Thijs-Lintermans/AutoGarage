using CoreBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.Models
{
    public class TimeSlotDataService
    {
        public static async Task<List<TimeSlot>> GetTimeSlotsAsync()
        {
            try
            {
                var timeSlots = await ApiService<List<TimeSlot>>.GetAsync("timeslots");
                return timeSlots;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching time slots: {ex.Message}", ex);
            }
        }

        public static async Task<List<TimeSlot>> GetAvailableTimeSlotsByDateAsync(DateOnly date)
        {
            try
            {
                var timeSlots = await GetTimeSlotsAsync();

                var availableTimeSlots = timeSlots.Where(t =>
                {
                    if (DateTime.TryParse(t.Appointments.FirstOrDefault()?.AppointmentDate, out var appointmentDate))
                    {
                        return appointmentDate.Date != date.ToDateTime(TimeOnly.MinValue).Date;
                    }
                    return true;
                }).ToList();

                return availableTimeSlots;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching available time slots for date {date}: {ex.Message}", ex);
            }
        }


        public static async Task<TimeSlot> GetTimeSlotByStartTimeAsync(string startTime)
        {
            try
            {
                // Get all time slots
                var timeSlots = await GetTimeSlotsAsync();

                // Find the matching time slot based on the StartTime string
                var selectedTimeSlot = timeSlots.FirstOrDefault(ts => ts.StartTime.ToString() == startTime);

                if (selectedTimeSlot == null)
                {
                    throw new Exception($"TimeSlot with StartTime {startTime} not found.");
                }

                return selectedTimeSlot;
            }
            catch (Exception ex)
            {
                // Error handling
                throw new Exception($"Error fetching time slot with StartTime {startTime}: {ex.Message}", ex);
            }
        }


        public static async Task<TimeSlot> GetTimeSlotByIdAsync(int id)
        {
            try
            {
                // Get the specific time slot by ID
                var timeSlot = await ApiService<TimeSlot>.GetAsync($"timeslots/{id}");

                if (timeSlot == null)
                {
                    throw new Exception($"TimeSlot with ID {id} not found.");
                }

                return timeSlot;
            }
            catch (Exception ex)
            {
                // Error handling
                throw new Exception($"Error fetching time slot with ID {id}: {ex.Message}", ex);
            }
        }
    }
}
