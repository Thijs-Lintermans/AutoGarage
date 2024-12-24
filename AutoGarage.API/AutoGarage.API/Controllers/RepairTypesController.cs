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
    public class RepairTypesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public RepairTypesController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // GET: api/RepairTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RepairType>>> GetRepairTypes()
        {
            var repairTypes = await _uow.RepairTypeRepository.GetAsync(
                filter: null,    // No filter applied
                orderBy: null,   // No specific ordering applied
                includes: new Expression<Func<RepairType, object>>[]
                {
                    r => r.Appointments    // Include Appointments for each repair type
                }
            );

            return Ok(repairTypes); // Return the result directly
        }

        // GET: api/RepairTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RepairType>> GetRepairType(int id)
        {
            var repairType = await _uow.RepairTypeRepository.GetAsync(
                filter: r => r.RepairTypeId == id, // Filter by the repair type ID
                orderBy: null,                    // No ordering needed
                includes: new Expression<Func<RepairType, object>>[]
                {
                    r => r.Appointments    // Include Appointments for this specific repair type
                }
            );

            var result = repairType.FirstOrDefault(); // Since GetAsync returns IEnumerable, we use FirstOrDefault to get the single entity

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result); // Return the repair type with its appointments
        }

        // PUT: api/RepairTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRepairType(int id, RepairType repairType)
        {
            if (id != repairType.RepairTypeId)
            {
                return BadRequest("Repair Type ID mismatch.");
            }

            try
            {
                await _uow.RepairTypeRepository.UpdateAsync(repairType); // Use unit of work's repository
                await _uow.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await RepairTypeExists(id))  // Add 'await' here
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Return NoContent on successful update
        }

        // POST: api/RepairTypes
        [HttpPost]
        public async Task<ActionResult<RepairType>> PostRepairType(RepairType repairType)
        {
            try
            {
                await _uow.RepairTypeRepository.InsertAsync(repairType); // Use unit of work's repository
                await _uow.SaveAsync();
                return CreatedAtAction(nameof(GetRepairType), new { id = repairType.RepairTypeId }, repairType); // Return Created response
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating repair type: {ex.Message}"); // Return BadRequest on error
            }
        }

        // DELETE: api/RepairTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRepairType(int id)
        {
            var repairType = await _uow.RepairTypeRepository.FindAsync(id); // Use unit of work's repository
            if (repairType == null)
            {
                return NotFound();
            }

            await _uow.RepairTypeRepository.DeleteAsync(repairType); // Use unit of work's repository
            await _uow.SaveAsync();

            return NoContent(); // Return NoContent on successful deletion
        }

        private async Task<bool> RepairTypeExists(int id)
        {
            var repairTypes = await _uow.RepairTypeRepository.GetAsync(e => e.RepairTypeId == id); // Use unit of work's repository
            return repairTypes.Any(); // Return true if exists, otherwise false
        }
    }
}
