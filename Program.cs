using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Bogus;
using EfStress.Db;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace EfStress
{
    class Program
    {
        private static ILogger _logger;
        
        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }
        static async Task MainAsync()
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = config.Build();
            var loggerFactory = LoggerFactory.Create(builder => builder
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddConsole());

            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
                .UseLoggerFactory(loggerFactory)
                .UseSqlServer(configuration.GetConnectionString("SqlDatabase"), options => options
                    .EnableRetryOnFailure());

            _logger = loggerFactory.CreateLogger<Program>();

            try
            {
                await using var ctx = new TestDbContext(optionsBuilder.Options);
                // Migrations are done manually / Kyler doesn't
                await ctx.Database.MigrateAsync();
                var fakerTestRecord = new Faker<TestRecord>()
                    .RuleFor(o => o.Id, Guid.NewGuid)
                    .RuleFor(o => o.Firstname, f => f.Name.FirstName())
                    .RuleFor(o => o.Email, f => f.Internet.Email());

                var testRecords = fakerTestRecord.Generate(Int32.Parse(configuration["create-records"]));

                _logger.LogInformation("{date} : Deleting last run ...", DateTime.Now);
                string cmd = $"DELETE FROM [TestRecords]";
                ctx.Database.ExecuteSqlRaw(cmd);

                _logger.LogInformation("[SQLCMD] {date} : Insert & Select for {recordCount} Records ...", DateTime.Now,
                    String.Format("{0:n0}", testRecords.Count));
                await RawSqlInsert(configuration.GetConnectionString("SqlDatabase"), testRecords, Convert.ToInt32(configuration["log-every"]));
                _logger.LogInformation("[SQLCMD] {date} : Completed Insert & Select", DateTime.Now);

                // _logger.LogInformation("[EF] {date} : Insert & Select for {recordCount} Records ...", DateTime.Now, String.Format("{0:n0}", testRecords.Count));
                // await SqlInsertEf(ctx, testRecords);
                // _logger.LogInformation("[EF] {date} : Completed Insert & Select", DateTime.Now);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception occured when applying EF migrations.");
                throw;
            }

            _logger.LogInformation("Test Complete!!");

            _ = Console.ReadKey();
        }

        private static async Task SqlInsertEf(TestDbContext ctx, List<TestRecord> testRecords)
        {
            int currRec = 0;
            foreach (var testRecord in testRecords)
            {
                if (currRec % 100 == 0)
                {
                    _logger.LogInformation("[EF] {date} : Inserted {currRec} records",DateTime.Now, currRec);
                }
                
                await ctx.TestRecords.AddAsync(testRecord);
                await ctx.SaveChangesAsync(); // This was asked for :)
                _ = await ctx.TestRecords.FirstAsync(x => x.Id == testRecord.Id); // So was this :)
                currRec++;
            }
        }

        private static async Task RawSqlInsert(string connectionString, List<TestRecord> records, int logevery=1000)
        {
            String query = "INSERT INTO dbo.TestRecords (id,Firstname,Email) VALUES (@id, @Firstname, @Email)";
            int currRec = 0;
            foreach (var record in records)
            {
                if (currRec % logevery == 0)
                {
                    _logger.LogInformation("[SQL] {date} : Inserted {currRec} records",DateTime.Now, currRec);
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand insertCommand = new SqlCommand(query, connection);
                    insertCommand.CommandType = CommandType.Text;
                
                    insertCommand.Parameters.AddWithValue("@id", record.Id);
                    insertCommand.Parameters.AddWithValue("@Firstname", record.Firstname);
                    insertCommand.Parameters.AddWithValue("@Email", record.Email);
                    
                    var str = insertCommand.CommandText;
                    string rawQueryWithParams = insertCommand.CommandText;

                    foreach (SqlParameter p in insertCommand.Parameters)
                    {
                        rawQueryWithParams = rawQueryWithParams.Replace(p.ParameterName, p.Value.ToString());
                    }
                    insertCommand.ExecuteNonQuery();
                    
                    SqlCommand readCommand = new SqlCommand("SELECT * FROM TestRecords where id = @id", connection);
                    readCommand.Parameters.AddWithValue("@id", record.Id);
                    _ = await readCommand.ExecuteReaderAsync();
                    currRec++;
                }
            }
        }
    }
}