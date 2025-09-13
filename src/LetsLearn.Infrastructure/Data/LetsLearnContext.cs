using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsLearn.Core.Entities;
namespace LetsLearn.Infrastructure.Data
{
    public class LetsLearnContext : DbContext
    {
        #region Ctors
        public LetsLearnContext(DbContextOptions<LetsLearnContext> options) : base(options)
        {
        }
        #endregion
        // Define DbSets for your entities here, e.g.:
        #region DbSets
        public DbSet<WeatherForecast> WeatherForecasts { get; set; }
        #endregion

        #region OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        #endregion
    }
}
