using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Repository;
using LetsLearn.Infrastructure.Data;
namespace LetsLearn.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LetsLearnContext _context;

        public IRepository<WeatherForecast> WeatherForecasts { get; private set; }

        public ICourseRepository Course { get; private set; }

        public IRepository<CloudinaryFile> CloudinaryFiles { get; private set; }
        public IUserRepository Users { get; private set; }  
        public IRefreshTokenRepository RefreshTokens { get; private set; }
        public IMessageRepository Messages { get; private set; }
        public IConversationRepository Conversations { get; private set; }
        public IQuestionRepository Questions { get; }
        public IQuestionChoiceRepository QuestionChoices { get; }
        public ISectionRepository Sections { get; private set; }
        public ITopicRepository Topics { get; private set; }
        public ITopicPageRepository TopicPages { get; private set; }
        public ITopicFileRepository TopicFiles { get; private set; }
        public ITopicLinkRepository TopicLinks { get; private set; }
        public ITopicQuizRepository TopicQuizzes { get; private set; }
        public ITopicAssignmentRepository TopicAssignments { get; private set; }
        public IRepository<AssignmentResponse> AssignmentResponses { get; private set; }
        public IRepository<TopicQuizQuestion> TopicQuizQuestions { get; private set; }
        public IRepository<TopicQuizQuestionChoice> TopicQuizQuestionChoices { get; private set; }
        public IRepository<Enrollment> Enrollments { get; private set; }
        public UnitOfWork(LetsLearnContext context)
        {
            _context = context;
            WeatherForecasts = new GenericRepository<WeatherForecast>(_context);
            Course = new CourseRepository(_context);
            CloudinaryFiles = new GenericRepository<CloudinaryFile>(_context);
            Users = new UserRepository(_context); 
            RefreshTokens = new RefreshTokenRepository(_context);
            Messages = new MessageRepository(_context);
            Conversations = new ConversationRepository(_context);
            Sections = new SectionRepository(_context);
            Topics = new TopicRepository(_context);
            TopicPages = new TopicPageRepository(_context);
            TopicFiles = new TopicFileRepository(_context);
            TopicLinks = new TopicLinkRepository(_context);
            TopicQuizzes = new TopicQuizRepository(_context);
            TopicAssignments = new TopicAssignmentRepository(_context);
            Questions = new QuestionRepository(_context);
            QuestionChoices = new QuestionChoiceRepository(_context);
            AssignmentResponses = new GenericRepository<AssignmentResponse>(_context);
            Enrollments = new GenericRepository<Enrollment>(_context);
        }

        public async Task<int> CommitAsync() =>
            await _context.SaveChangesAsync();

        public async ValueTask DisposeAsync() =>
            await _context.DisposeAsync();
    }
}
