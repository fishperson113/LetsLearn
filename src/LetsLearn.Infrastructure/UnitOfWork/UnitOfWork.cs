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

        public UnitOfWork(LetsLearnContext context)
        {
            _context = context;
            WeatherForecasts = new GenericRepository<WeatherForecast>(_context);
            Course = new CourseRepository(_context);
            CloudinaryFiles = new GenericRepository<CloudinaryFile>(_context);
            Users = new UserRepository(_context); 
            RefreshTokens = new RefreshTokenRepository(_context);
        }

        public async Task<int> CommitAsync() =>
            await _context.SaveChangesAsync();

        public async ValueTask DisposeAsync() =>
            await _context.DisposeAsync();
    }
}
