using Microsoft.Extensions.Logging;
using REBUSS.GitDaif.Service.API.Services;

namespace REBUSS.GitDaif.Service.API.IntegrationTests.Services
{
    [TestFixture]
    [Category("Unit")]
    [Category("BackgroundService")]
    public class DiffFileCleanerBackgroundServiceTests
    {
        private string _testDirectory;
        private ILogger<DiffFileCleanerBackgroundService> _logger;

        [SetUp]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"DiffCleanerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<DiffFileCleanerBackgroundService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Test]
        public void Constructor_WithNullDirectory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DiffFileCleanerBackgroundService(null, _logger));
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DiffFileCleanerBackgroundService(_testDirectory, null));
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesService()
        {
            // Act
            var service = new DiffFileCleanerBackgroundService(_testDirectory, _logger);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public async Task ExecuteAsync_CleansOldFiles_AndKeepsNewFiles()
        {
            // Arrange
            var service = new DiffFileCleanerBackgroundService(_testDirectory, _logger);
            
            // Create old file (yesterday)
            var oldFile = Path.Combine(_testDirectory, "old_file.diff.txt");
            File.WriteAllText(oldFile, "old content");
            File.SetCreationTime(oldFile, DateTime.Now.AddDays(-2));

            // Create new file (today)
            var newFile = Path.Combine(_testDirectory, "new_file.diff.txt");
            File.WriteAllText(newFile, "new content");

            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            await Task.Delay(2000); // Wait for initial cleanup
            cts.Cancel();

            try
            {
                await executeTask;
            }
            catch (TaskCanceledException)
            {
                // Expected when canceling
            }

            // Assert
            Assert.That(File.Exists(oldFile), Is.False, "Old file should be deleted");
            Assert.That(File.Exists(newFile), Is.True, "New file should be kept");
        }

        [Test]
        public async Task ExecuteAsync_WithNonExistentDirectory_HandlesGracefully()
        {
            // Arrange
            var nonExistentDir = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}");
            var service = new DiffFileCleanerBackgroundService(nonExistentDir, _logger);
            var cts = new CancellationTokenSource();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                var executeTask = service.StartAsync(cts.Token);
                await Task.Delay(1000);
                cts.Cancel();
                
                try
                {
                    await executeTask;
                }
                catch (TaskCanceledException)
                {
                    // Expected
                }
            });
        }

        [Test]
        public async Task ExecuteAsync_OnlyDeletesDiffTxtFiles()
        {
            // Arrange
            var service = new DiffFileCleanerBackgroundService(_testDirectory, _logger);
            
            // Create old diff file
            var oldDiffFile = Path.Combine(_testDirectory, "old.diff.txt");
            File.WriteAllText(oldDiffFile, "old diff");
            File.SetCreationTime(oldDiffFile, DateTime.Now.AddDays(-2));

            // Create old non-diff file
            var oldOtherFile = Path.Combine(_testDirectory, "old.log");
            File.WriteAllText(oldOtherFile, "old log");
            File.SetCreationTime(oldOtherFile, DateTime.Now.AddDays(-2));

            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            await Task.Delay(2000);
            cts.Cancel();

            try
            {
                await executeTask;
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            // Assert
            Assert.That(File.Exists(oldDiffFile), Is.False, "Old diff file should be deleted");
            Assert.That(File.Exists(oldOtherFile), Is.True, "Old non-diff file should be kept");
        }

        [Test]
        public async Task ExecuteAsync_DeletesFilesOlderThanToday()
        {
            // Arrange
            var service = new DiffFileCleanerBackgroundService(_testDirectory, _logger);
            
            // Create file from yesterday
            var yesterdayFile = Path.Combine(_testDirectory, "yesterday.diff.txt");
            File.WriteAllText(yesterdayFile, "yesterday");
            File.SetCreationTime(yesterdayFile, DateTime.Now.Date.AddDays(-1));

            // Create file from today
            var todayFile = Path.Combine(_testDirectory, "today.diff.txt");
            File.WriteAllText(todayFile, "today");
            File.SetCreationTime(todayFile, DateTime.Now.Date);

            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            await Task.Delay(2000);
            cts.Cancel();

            try
            {
                await executeTask;
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            // Assert
            Assert.That(File.Exists(yesterdayFile), Is.False, "Yesterday's file should be deleted");
            Assert.That(File.Exists(todayFile), Is.True, "Today's file should be kept");
        }

        [Test]
        public async Task StopAsync_StopsServiceGracefully()
        {
            // Arrange
            var service = new DiffFileCleanerBackgroundService(_testDirectory, _logger);
            var cts = new CancellationTokenSource();

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(500);
            
            // Assert - Should not throw
            Assert.DoesNotThrowAsync(async () => await service.StopAsync(cts.Token));
        }
    }
}
