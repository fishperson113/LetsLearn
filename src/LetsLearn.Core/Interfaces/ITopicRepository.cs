using LetsLearn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Core.Interfaces
{
    public interface  ITopicRepository : IRepository<Topic>
    {
        Task<IReadOnlyList<Topic>> GetAllBySectionIdAsync(Guid sectionId, CancellationToken ct = default);
        Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default);
        Task<Topic?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);
    }
}
