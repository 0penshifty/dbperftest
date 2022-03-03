using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfStress.Db
{
    public class TestDbConfiguration : IEntityTypeConfiguration<TestRecord>
    {
        public void Configure(EntityTypeBuilder<TestRecord> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
    
    public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost,1433;Database=efstress;User=sa;Password=r00tp@ssw0rD;");

            return new TestDbContext(optionsBuilder.Options);
        }
    }
}