using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.Extensions.Configuration;

namespace EfStress.Db
{
    public interface ITestDbContext
    {
        
    }
    
    public class TestDbContext : DbContext, ITestDbContext
    {
        public DbSet<TestRecord> TestRecords { get; set; }
        private readonly string _connectionString;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration iConfiguration = builder.Build();
            _connectionString = iConfiguration.GetConnectionString("SqlDatabase");
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlServer(_connectionString, providerOptions => providerOptions.CommandTimeout(60))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new TestDbConfiguration());
        }
    }
    
    public class TestRecord
    {
        [Key]
        public Guid Id { get; set; }
        public string Firstname { get; set; }
        public string Email { get; set; }
    }
}