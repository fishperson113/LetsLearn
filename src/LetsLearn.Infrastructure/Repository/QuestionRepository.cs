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
    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        public QuestionRepository(LetsLearnContext context) : base(context) { }

        public async Task<List<Question>> GetAllByCourseIdAsync(String courseId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(q => q.Choices)
                .Where(q => q.CourseId == courseId)
                .ToListAsync(ct);
        }

        public async Task<Question?> GetWithChoicesAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.Id == id, ct);
        }
    }
}
