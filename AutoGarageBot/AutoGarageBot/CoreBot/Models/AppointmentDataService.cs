using CoreBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBot.Models
{
    public class AppointmentDataService
    {
        public static async Task<List<Appointment>> GetAppointmentsAsync()
        {
            return await ApiService<List<Appointment>>.GetAsync($"appointments");
        }

        public async static Task InsertAppointmentAsync(Appointment appointment)
        {
            await ApiService<Appointment>.PostAsync("appointments", appointment);
        }

        public async static Task UpdateAppointmentAsync(Appointment appointment)
        {
            await ApiService<Appointment>.PutAsync($"appointments/{appointment.AppointmentId}", appointment);
        }
    }
}
