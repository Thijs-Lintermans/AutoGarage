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
    public class TimeSlotsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public TimeSlotsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // GET: api/TimeSlots
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeSlot>>> GetTimeSlots()
        {
            var timeSlots = await _uow.TimeSlotRepository.GetAllAsync(); // Use unit of work's repository
            return Ok(timeSlots); // Return the result directly
        }

        // GET: api/TimeSlots/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeSlot>> GetTimeSlot(int id)
        {
            var timeSlot = await _uow.TimeSlotRepository.FindAsync(id); // Use unit of work's repository

            if (timeSlot == null)
            {
                return NotFound();
            }

            return Ok(timeSlot); // Return the time slot if found
        }

        // PUT: api/TimeSlots/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTimeSlot(int id, TimeSlot timeSlot)
        {
            if (id != timeSlot.TimeSlotId)
            {
                return BadRequest("Time Slot ID mismatch.");
            }

            try
            {
                await _uow.TimeSlotRepository.UpdateAsync(timeSlot); // Use unit of work's repository
                await _uow.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TimeSlotExists(id)) // Add 'await' here
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

        // POST: api/TimeSlots
        [HttpPost]
        public async Task<ActionResult<TimeSlot>> PostTimeSlot(TimeSlot timeSlot)
        {
            try
            {
                await _uow.TimeSlotRepository.InsertAsync(timeSlot); // Use unit of work's repository
                await _uow.SaveAsync();
                return CreatedAtAction(nameof(GetTimeSlot), new { id = timeSlot.TimeSlotId }, timeSlot); // Return Created response
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating time slot: {ex.Message}"); // Return BadRequest on error
            }
        }

        // DELETE: api/TimeSlots/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            var timeSlot = await _uow.TimeSlotRepository.FindAsync(id); // Use unit of work's repository
            if (timeSlot == null)
            {
                return NotFound();
            }

            await _uow.TimeSlotRepository.DeleteAsync(timeSlot); // Use unit of work's repository
            await _uow.SaveAsync();

            return NoContent(); // Return NoContent on successful deletion
        }

        private async Task<bool> TimeSlotExists(int id)
        {
            var timeSlots = await _uow.TimeSlotRepository.GetAsync(e => e.TimeSlotId == id); // Use unit of work's repository
            return timeSlots.Any(); // Return true if exists, otherwise false
        }
    }
}
