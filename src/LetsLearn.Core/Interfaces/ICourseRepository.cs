using LetsLearn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Core.Interfaces
{
    public interface ICourseRepository : IRepository<Course>
    {
        Task <IEnumerable<Course?>> GetAllCoursesByIsPublishedTrue();
        Task <IEnumerable<Course?>> GetByCreatorId(Guid Id);
        Task<bool> ExistByTitle(string title);
    }
}
