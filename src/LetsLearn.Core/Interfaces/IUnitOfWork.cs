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
        IQuestionRepository Questions { get; }
        IQuestionChoiceRepository QuestionChoices { get; }
        Task<int> CommitAsync();
    }
}
