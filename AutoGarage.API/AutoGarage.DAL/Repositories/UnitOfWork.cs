using AutoGarage.DAL.Models;
using Parkeerwachter.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private AutoGarageContext _context;

        private IRepository<Customer> customerRepository;
        private IRepository<Appointment> appointmentRepository;
        private IRepository<TimeSlot> timeSlotRepository;
        private IRepository<RepairType> repairTypeRepository;

        public UnitOfWork(AutoGarageContext context)
        { 
            _context = context;
        }

        public IRepository<Customer> CustomerRepository
        {
            get
            {
                if (customerRepository == null)
                {
                    customerRepository = new GenericRepository<Customer>(_context);
                }
                return customerRepository;
            }
        }

        public IRepository<Appointment> AppointmentRepository
        {
            get
            {
                if (appointmentRepository == null)
                {
                    appointmentRepository = new GenericRepository<Appointment>(_context);
                }
                return appointmentRepository;
            }
        }

        public IRepository<TimeSlot> TimeSlotRepository
        {
            get
            {
                if (timeSlotRepository == null)
                {
                    timeSlotRepository = new GenericRepository<TimeSlot>(_context);
                }
                return timeSlotRepository;
            }
        }

        public IRepository<RepairType> RepairTypeRepository
        {
            get
            {
                if (repairTypeRepository == null)
                {
                    repairTypeRepository = new GenericRepository<RepairType>(_context);
                }
                return repairTypeRepository;
            }
        }
        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}

