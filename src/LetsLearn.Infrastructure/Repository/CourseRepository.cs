using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Infrastructure.Repository
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(LetsLearnContext context) : base(context)
        {
        }
        public async Task<Course?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return await _dbSet.Include(c => c.Sections)
                               .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<bool> ExistByTitle(string title)
        {
            return await ExistsAsync(c => c.Title == title);
        }

        public async Task<IEnumerable<Course?>> GetAllCoursesByIsPublishedTrue()
        {
            return await FindAsync(c => c.IsPublished == true);
        }

        public async Task<IEnumerable<Course?>> GetByCreatorId(Guid id)
        {
            return await FindAsync(c => c.CreatorId == id);
        }
    }
}
