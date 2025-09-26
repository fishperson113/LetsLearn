using LetsLearn.Infrastructure.Data;
using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Infrastructure.Repository
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(LetsLearnContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct);
        }
    }
}
