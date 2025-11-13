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
    public class TopicQuizRepository : GenericRepository<TopicQuiz>, ITopicQuizRepository
    {
        public TopicQuizRepository(LetsLearnContext context) : base(context) { }
        public async Task UpdateAsync(TopicQuiz topic)
        {
            _context.Entry(topic).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<TopicQuiz?> GetWithQuestionsAsync(Guid topicId)
        {
            return await _context.TopicQuizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                    .ThenInclude(c => c.Choices)
                .FirstOrDefaultAsync(q => q.TopicId == topicId);
        }
    }
}
