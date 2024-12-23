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
            return await ApiService<List<TimeSlot>>.GetAsync($"timeslots/available/{date}");
        }
    }
}
