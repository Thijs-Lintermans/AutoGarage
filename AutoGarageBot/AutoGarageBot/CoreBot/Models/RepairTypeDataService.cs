using CoreBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBot.Models
{
    public class RepairTypeDataService
    {
        public static async Task<List<RepairType>> GetRepairTypesAsync()
        {
            return await ApiService<List<RepairType>>.GetAsync($"repairtypes");
        }
        public static async Task<RepairType> GetRepairTypeByIdAsync(int id)
        {
            return await ApiService<RepairType>.GetAsync($"repairtypes/{id}");
        }
    }
}
