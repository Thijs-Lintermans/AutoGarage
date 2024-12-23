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
            var customers = await _uow.CustomerRepository.GetAllAsync(); // Use unit of work's repository
            return customers.ToList();
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _uow.CustomerRepository.FindAsync(id); // Use unit of work's repository

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
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
