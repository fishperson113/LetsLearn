using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
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
