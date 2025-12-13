using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Test.Integration
{
    public class AuthServiceIntegrationTests : IntegrationTestBase
    {
        protected override void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        }

        [Fact]
        public async Task RegisterAsync_CreatesUserInDatabase()
        {
            await using var scope = _provider.CreateAsyncScope();

            var service = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var ctx = new DefaultHttpContext();

            await service.RegisterAsync(new SignUpRequest
            {
                Email = "test@mail.com",
                Username = "test",
                Password = "123",
                Role = "STUDENT"
            }, ctx);

            var user = await uow.Users.FirstOrDefaultAsync(u => u.Email == "test@mail.com");
            Assert.NotNull(user);
        }
    }
}
