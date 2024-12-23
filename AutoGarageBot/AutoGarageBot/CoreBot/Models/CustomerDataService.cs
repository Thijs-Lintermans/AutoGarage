using CoreBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.Models
{
    public class CustomerDataService
    {
        public static async Task<List<Customer>> GetCustomersAsync()
        {
            return await ApiService<List<Customer>>.GetAsync($"customers");
        }
        public static async Task<Customer> GetCustomerByLicenseplateAsync(string licensePlate)
        {
            try
            {
                // Call the API endpoint for customer lookup by license plate
                var customer = await ApiService<Customer>.GetAsync($"customers/licenseplate/{licensePlate}");

                return customer;  // Return the found customer (or null if not found)
            }
            catch (Exception ex)
            {
                // Handle any exceptions (such as network errors, etc.)
                throw new Exception($"Error fetching customer by license plate: {ex.Message}", ex);
            }
        }


        public async static Task InsertCustomerAsync(Customer customer)
        {
            await ApiService<Customer>.PostAsync("customers", customer);
        }

        public async static Task UpdateCustomerAsync(Customer customer)
        {
            await ApiService<Customer>.PutAsync($"customers/{customer.CustomerId}", customer);
        }

    }
}
