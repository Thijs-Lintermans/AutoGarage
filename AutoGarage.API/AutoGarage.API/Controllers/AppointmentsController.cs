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
    public class AppointmentsController : ControllerBase
    {
        private IUnitOfWork _uow;

        public AppointmentsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // GET: api/Appointments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
        {
            var appointments = await _uow.AppointmentRepository.GetAllAsync();
            return appointments.ToList();
        }

        // GET: api/Appointments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Appointment>> GetAppointment(int id)
        {
            var appointment = await _uow.AppointmentRepository.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            return appointment;
        }

        // PUT: api/Appointments/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // PUT: api/Appointments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAppointment(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return BadRequest();
            }

            await _uow.AppointmentRepository.UpdateAsync(appointment);

            try
            {
                await _uow.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AppointmentExists(id))  // Add 'await' here
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


        // POST: api/Appointments
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Appointment>> PostAppointment(Appointment appointment)
        {
            await _uow.AppointmentRepository.InsertAsync(appointment);
            await _uow.SaveAsync();

            return CreatedAtAction("GetAppointment", new { id = appointment.AppointmentId }, appointment);
        }

        // DELETE: api/Appointments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _uow.AppointmentRepository.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            await _uow.AppointmentRepository.DeleteAsync(appointment);
            await _uow.SaveAsync();

            return NoContent();
        }

        private async Task<bool> AppointmentExists(int id)
        {
            var appointments = await _uow.AppointmentRepository.GetAsync(e => e.AppointmentId == id);
            return appointments.Any();
        }
    }
}
