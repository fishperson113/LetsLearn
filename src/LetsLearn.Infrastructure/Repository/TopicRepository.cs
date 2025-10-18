﻿using LetsLearn.Core.Entities;
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
    public class TopicRepository : GenericRepository<Topic>, ITopicRepository
    {
        public TopicRepository(LetsLearnContext context) : base(context) { }

        public async Task<IReadOnlyList<Topic>> GetAllBySectionIdAsync(Guid sectionId, CancellationToken ct = default)
            => await _dbSet.AsNoTracking()
                           .Where(t => t.SectionId == sectionId)
                           .ToListAsync(ct);

        public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default)
            => await _dbSet.AnyAsync(t => t.Id == id, ct);

        public async Task<Topic?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
