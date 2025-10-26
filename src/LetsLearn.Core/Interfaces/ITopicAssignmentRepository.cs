using LetsLearn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Core.Interfaces
{
    public interface ITopicAssignmentRepository : IRepository<TopicAssignment>
    {
        Task UpdateAsync(TopicAssignment topic);
    }
}
