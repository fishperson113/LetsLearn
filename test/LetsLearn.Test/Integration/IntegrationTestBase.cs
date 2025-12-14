using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
using LetsLearn.Infrastructure.Repository;
using LetsLearn.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Test.Integration
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly ServiceProvider _provider;

        protected IntegrationTestBase()
        {
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Secret"] = "THIS_IS_A_VERY_LONG_SECRET_KEY_32_BYTES_MIN",
                    ["Jwt:AccessTokenExpireSeconds"] = "3600",
                    ["Jwt:RefreshTokenExpireSeconds"] = "7200",
                    ["Jwt:AccessTokenCookie"] = "ACCESS_TOKEN",
                    ["Jwt:RefreshTokenCookie"] = "REFRESH_TOKEN",
                    ["Jwt:AuthPrefix"] = "Bearer_"
                })
                .Build();

            services.AddLogging();

            services.AddSingleton<IConfiguration>(config);

            services.AddDbContext<LetsLearnContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IQuizResponseRepository, QuizResponseRepository>();
            services.AddScoped<IAssignmentResponseRepository, AssignmentResponseRepository>();

            RegisterServices(services);

            _provider = services.BuildServiceProvider();
        }

        protected abstract void RegisterServices(IServiceCollection services);

        public void Dispose()
        {
            _provider.Dispose();
        }
    }

}
