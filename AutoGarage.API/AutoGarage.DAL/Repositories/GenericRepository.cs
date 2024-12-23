using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Parkeerwachter.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AutoGarage.DAL.Repositories
{
    public class GenericRepository<T>: IRepository<T> where T : class
    {
        private AutoGarageContext _context;
        private DbSet<T> table = null;
        public GenericRepository(AutoGarageContext context)
        {
            _context = context;
            table = _context.Set<T>();
        }

        virtual public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await table.ToListAsync();
        }

        virtual public async Task<T> FindAsync(int id)
        {
            return await table.FindAsync(id);
        }

        public async Task<T> GetByIDAsync(int id)
        {
            return await table.FindAsync(id);
        }

        public async Task InsertAsync(T obj)
        {
            await table.AddAsync(obj);
        }

        public async Task UpdateAsync(T obj)
        {
            table.Update(obj);
            await _context.SaveChangesAsync(); // Ensure changes are saved asynchronously
        }
        public async Task DeleteAsync(T obj)
        {
            table.Remove(obj);
            await _context.SaveChangesAsync(); // Ensure changes are saved asynchronously
        }
        public async Task<IEnumerable<T>> GetAsync(
              Expression<Func<T, bool>> filter = null,
              Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
              params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = table;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync();
        }
    }
}
