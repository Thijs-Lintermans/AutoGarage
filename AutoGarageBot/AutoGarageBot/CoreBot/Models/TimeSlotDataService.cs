using CoreBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBot.Models
{
    public class TimeSlotDataService
    {
        public static async Task<List<TimeSlot>> GetTimeSlotsAsync()
        {
            return await ApiService<List<TimeSlot>>.GetAsync($"timeslots");
        }

        public static async Task<List<TimeSlot>> GetTimeSlotsByDateAsync(DateOnly date)
        {
            // Format the DateOnly object to a string that the API expects(e.g., "yyyy-MM-dd")
            string formattedDate = date.ToString("yyyy-MM-dd");

            // Make the API call
            var timeSlots = await ApiService<List<TimeSlot>>.GetAsync($"timeslots/{formattedDate}");

            return timeSlots;
        }
    }
}
