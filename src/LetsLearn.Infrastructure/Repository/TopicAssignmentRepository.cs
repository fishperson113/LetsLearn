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
    public class TopicAssignmentRepository : GenericRepository<TopicAssignment>, ITopicAssignmentRepository
    {
        public TopicAssignmentRepository(LetsLearnContext context) : base(context) { }
        public async Task UpdateAsync(TopicAssignment topic)
        {
            _context.Entry(topic).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<List<TopicAssignment>> GetAssignmentsByCourseIdAndDateRangeAsync(Guid courseId, DateTime startTime, DateTime endTime, CancellationToken ct = default)
        {
            var assignments = await _context.TopicAssignments
                .Where(a => a.Open >= startTime &&
                            a.Close <= endTime)
                .ToListAsync(ct);

            return assignments;
        }
    }
}
