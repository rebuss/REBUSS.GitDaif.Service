using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using REBUSS.GitDaif.Service.API.Properties;

namespace REBUSS.GitDaif.Service.API.IntegrationTests.Fixtures
{
    public class TestFixtureBase
    {
        protected IConfiguration Configuration { get; private set; }
        protected AppSettings AppSettings { get; private set; }
        protected string TestOutputDirectory { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            Configuration = BuildConfiguration();
            AppSettings = Configuration.Get<AppSettings>();
            
            // Create test output directory
            TestOutputDirectory = Path.Combine(Path.GetTempPath(), $"GitDaifTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(TestOutputDirectory);
            
            // Override diff files directory for tests
            AppSettings.DiffFilesDirectory = TestOutputDirectory;
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up test directory
            if (Directory.Exists(TestOutputDirectory))
            {
                try
                {
                    Directory.Delete(TestOutputDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        protected IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
                .Build();
        }

        protected ILogger<T> CreateLogger<T>()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<T>();
        }

        protected IOptions<T> CreateOptions<T>(T options) where T : class
        {
            return Options.Create(options);
        }
    }
}
