using AutoGarage.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Repositories
{
    public interface IUnitOfWork
    {
        IRepository<Customer> CustomerRepository { get; }

        IRepository<Appointment> AppointmentRepository { get; }

        IRepository<TimeSlot> TimeSlotRepository { get; }
        IRepository<RepairType> RepairTypeRepository { get; }

        void Save();

        Task SaveAsync();
    }
}
