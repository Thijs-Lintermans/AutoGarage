using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarage.DAL.Models;
using Parkeerwachter.DAL;
using AutoGarage.DAL.Repositories;
using System.Linq.Expressions;

namespace AutoGarage.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public CustomersController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            var customers = await _uow.CustomerRepository.GetAsync(
                filter: null,    // No specific filter applied (you can add one if needed)
                orderBy: null,   // No specific ordering applied (you can add one if needed)
                includes: new Expression<Func<Customer, object>>[]
                {
            c => c.Appointments    // Include Appointments for each customer
                }
            );

            return customers.ToList();  // Return the list of customers with included appointments
        }


        // GET: api/Customers/5
        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _uow.CustomerRepository.GetAsync(
                filter: c => c.CustomerId == id,    // Filter by CustomerId
                orderBy: null,   // No specific ordering applied
                includes: new Expression<Func<Customer, object>>[]
                {
                    c => c.Appointments    // Include Appointments
                }
            );

            if (customer == null || !customer.Any())  // Check if the result is null or empty
            {
                return NotFound();
            }

            return customer.FirstOrDefault();  // Since GetAsync returns a list, return the first element
        }

        [HttpGet("licenseplate/{licensePlate}")]
        public async Task<ActionResult<Customer>> GetCustomerByLicensePlateAsync(string licensePlate)
        {
            // Fetch customer by license plate
            var customer = await _uow.CustomerRepository.GetAsync(
                filter: c => c.LicensePlate.Equals(licensePlate, StringComparison.OrdinalIgnoreCase),
                orderBy: null,
                includes: new Expression<Func<Customer, object>>[]
                {
                    c => c.Appointments    // Include Appointments
                }
            );

            if (customer == null || !customer.Any())
            {
                return NotFound();  // Return NotFound if no customer matches the license plate
            }

            return customer.FirstOrDefault();  // Return the first matching customer
        }



        // PUT: api/Customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            await _uow.CustomerRepository.UpdateAsync(customer); // Use unit of work's repository

            try
            {
                await _uow.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CustomerExists(id))  // Add 'await' here
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Customers
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            await _uow.CustomerRepository.InsertAsync(customer); // Use unit of work's repository
            await _uow.SaveAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.CustomerId }, customer);
        }

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _uow.CustomerRepository.FindAsync(id); // Use unit of work's repository
            if (customer == null)
            {
                return NotFound();
            }

            await _uow.CustomerRepository.DeleteAsync(customer); // Use unit of work's repository
            await _uow.SaveAsync();

            return NoContent();
        }

        private async Task<bool> CustomerExists(int id)
        {
            var customers = await _uow.CustomerRepository.GetAsync(e => e.CustomerId == id); // Use unit of work's repository
            return customers.Any();
        }
    }
}
