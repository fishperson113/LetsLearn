using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsLearn.Core.Entities;
namespace LetsLearn.Core.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IRepository<WeatherForecast> WeatherForecasts { get; }
        ICourseRepository Course { get; }
        IRepository<CloudinaryFile> CloudinaryFiles { get; }
        IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IMessageRepository Messages { get; }
        IConversationRepository Conversations { get; }
        IQuestionRepository Questions { get; }
        IQuestionChoiceRepository QuestionChoices { get; }
        ISectionRepository Sections { get; }
        ITopicRepository Topics { get; }
        ITopicPageRepository TopicPages { get; }
        ITopicFileRepository TopicFiles { get; }
        ITopicLinkRepository TopicLinks { get; }
        ITopicQuizRepository TopicQuizzes { get; }
        ITopicAssignmentRepository TopicAssignments { get; }
        IRepository<AssignmentResponse> AssignmentResponses { get; }
        IRepository<TopicQuizQuestion> TopicQuizQuestions { get; }
        IRepository<TopicQuizQuestionChoice> TopicQuizQuestionChoices { get; }

        IEnrollmentRepository Enrollments { get; }
        Task<int> CommitAsync();
    }
}
