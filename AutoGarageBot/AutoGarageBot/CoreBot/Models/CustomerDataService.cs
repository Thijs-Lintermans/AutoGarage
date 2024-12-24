using CoreBot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

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
                var response = await ApiService<Customer>.GetAsync($"customers/licenseplate/{licensePlate}");

                // Check for 404 status and handle silently
                if (response == null) // or response.StatusCode == HttpStatusCode.NotFound
                {
                    // Return null if customer not found
                    return null;
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                // Log other errors silently if needed
                Console.WriteLine("Request error: " + ex.Message);
                return null;  // Ensure a return value is provided even if there's an exception
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