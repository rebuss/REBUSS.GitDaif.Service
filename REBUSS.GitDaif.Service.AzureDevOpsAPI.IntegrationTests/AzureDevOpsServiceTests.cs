using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using REBUSS.GitDaif.Service.AzureDevOpsAPI;
using REBUSS.GitDaif.Service.AzureDevOpsAPI.Services;

namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.IntegrationTests
{
    [TestFixture]
    public class AzureDevOpsServiceTests
    {
        private AzureDevOpsService _azureDevOpsService;
        private IAzureDevOpsApiClient _apiClient;
        private IConfiguration _configuration;
        private int _testPullRequestId;
        private AzureDevOpsOptions _options;

        [SetUp]
        public void Setup()
        {
            _configuration = BuildConfiguration();
            _testPullRequestId = int.Parse(_configuration["TestSettings:PullRequestId"] ?? "2");

            // Configure options
            _options = new AzureDevOpsOptions
            {
                OrganizationName = _configuration["AzureDevOps:OrganizationName"] ?? string.Empty,
                ProjectName = _configuration["AzureDevOps:ProjectName"] ?? string.Empty,
                RepositoryName = _configuration["AzureDevOps:RepositoryName"] ?? string.Empty,
                PersonalAccessToken = _configuration["AzureDevOps:PersonalAccessToken"] ?? string.Empty,
                OutputDirectory = _configuration["AzureDevOps:OutputDirectory"] ?? string.Empty
            };

            var optionsWrapper = Options.Create(_options);

            // Create logger
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var apiClientLogger = loggerFactory.CreateLogger<AzureDevOpsApiClient>();
            var serviceLogger = loggerFactory.CreateLogger<AzureDevOpsService>();

            // Create HttpClient and API client
            var httpClient = new HttpClient();
            _apiClient = new AzureDevOpsApiClient(httpClient, optionsWrapper, apiClientLogger);

            // Create service
            _azureDevOpsService = new AzureDevOpsService(_apiClient, optionsWrapper, serviceLogger);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files if needed
            if (Directory.Exists(_options.OutputDirectory))
            {
                var testFiles = Directory.GetFiles(_options.OutputDirectory, $"PR_{_testPullRequestId}_*.diff");
                foreach (var file in testFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        private IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
                .Build();
        }

        [Test]
        [Category("Integration")]
        public async Task GetPullRequestDiffAsync_Should_Return_Diff_From_AzureDevOps_API()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _apiClient.GetPullRequestDiffAsync(_testPullRequestId);

            // Assert
            Assert.That(result, Is.Not.Null, "Diff content should not be null");
            Assert.That(result, Is.Not.Empty, "Diff content should not be empty");
            Assert.That(result, Does.Contain("changeEntries"), "Diff content should contain 'changeEntries' from API response");
            Assert.That(result, Does.Contain("changeType"), "Diff content should contain 'changeType' field");
            Assert.That(result, Does.Contain("item"), "Diff content should contain 'item' field with file information");
        }

        [Test]
        [Category("Integration")]
        public async Task GetPullRequestDetailsAsync_Should_Return_PR_Details_From_AzureDevOps_API()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _apiClient.GetPullRequestDetailsAsync(_testPullRequestId);

            // Assert
            Assert.That(result, Is.Not.Null, "PR details should not be null");
            Assert.That(result, Is.Not.Empty, "PR details should not be empty");
            Assert.That(result, Does.Contain("pullRequestId").Or.Contains("title"), 
                "PR details should contain pull request information");
        }

        [Test]
        [Category("Integration")]
        public async Task GetPullRequestDiffContentAsync_Should_Return_Formatted_Diff()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _azureDevOpsService.GetPullRequestDiffContentAsync(_testPullRequestId);

            // Assert
            Assert.That(result, Is.Not.Null, "Formatted diff should not be null");
            Assert.That(result, Is.Not.Empty, "Formatted diff should not be empty");
            Assert.That(result, Does.Contain($"Pull Request #{_testPullRequestId}"), 
                "Formatted diff should contain PR number");
            Assert.That(result, Does.Contain("DIFF CONTENT:"), 
                "Formatted diff should contain diff section header");
        }

        [Test]
        [Category("Integration")]
        public async Task SavePullRequestDiffAsync_Should_Save_Diff_To_File()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _azureDevOpsService.SavePullRequestDiffAsync(_testPullRequestId);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.PullRequestId, Is.EqualTo(_testPullRequestId), 
                "Result should contain correct PR ID");
            Assert.That(result.FilePath, Is.Not.Empty, "File path should not be empty");
            Assert.That(File.Exists(result.FilePath), Is.True, 
                $"Diff file should exist at {result.FilePath}");
            Assert.That(result.Message, Does.Contain("successfully"), 
                "Result message should indicate success");

            // Verify file content
            var fileContent = await File.ReadAllTextAsync(result.FilePath);
            Assert.That(fileContent, Is.Not.Empty, "Saved file should not be empty");
            Assert.That(fileContent, Does.Contain($"Pull Request #{_testPullRequestId}"), 
                "Saved file should contain PR number");
        }

        [Test]
        [Category("Integration")]
        public async Task SavePullRequestDiffAsync_Should_Create_Output_Directory_If_Not_Exists()
        {
            // Arrange
            var testOutputDir = Path.Combine(Path.GetTempPath(), $"AzureDevOpsTest_{Guid.NewGuid()}");
            _options.OutputDirectory = testOutputDir;

            var optionsWrapper = Options.Create(_options);
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var apiClientLogger = loggerFactory.CreateLogger<AzureDevOpsApiClient>();
            var serviceLogger = loggerFactory.CreateLogger<AzureDevOpsService>();
            var httpClient = new HttpClient();
            var apiClient = new AzureDevOpsApiClient(httpClient, optionsWrapper, apiClientLogger);
            var service = new AzureDevOpsService(apiClient, optionsWrapper, serviceLogger);

            try
            {
                // Act
                var result = await service.SavePullRequestDiffAsync(_testPullRequestId);

                // Assert
                Assert.That(Directory.Exists(testOutputDir), Is.True, 
                    "Output directory should be created if it doesn't exist");
                Assert.That(File.Exists(result.FilePath), Is.True, 
                    "Diff file should be saved in the created directory");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testOutputDir))
                {
                    Directory.Delete(testOutputDir, true);
                }
            }
        }

        [Test]
        [Category("Integration")]
        public void GetPullRequestDiffAsync_Should_Throw_HttpRequestException_For_Invalid_PR_ID()
        {
            // Arrange
            int invalidPrId = -1;

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await _apiClient.GetPullRequestDiffAsync(invalidPrId),
                "Should throw HttpRequestException for invalid PR ID");
        }

        [Test]
        [Category("Integration")]
        public void AzureDevOpsService_Should_Validate_Configuration_On_Creation()
        {
            // Arrange
            var invalidOptions = new AzureDevOpsOptions
            {
                OrganizationName = "",
                ProjectName = "",
                RepositoryName = "",
                PersonalAccessToken = "",
                OutputDirectory = ""
            };

            var optionsWrapper = Options.Create(invalidOptions);
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var apiClientLogger = loggerFactory.CreateLogger<AzureDevOpsApiClient>();
            var serviceLogger = loggerFactory.CreateLogger<AzureDevOpsService>();
            var httpClient = new HttpClient();
            var apiClient = new AzureDevOpsApiClient(httpClient, optionsWrapper, apiClientLogger);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                new AzureDevOpsService(apiClient, optionsWrapper, serviceLogger),
                "Should throw InvalidOperationException for invalid configuration");
        }

        [Test]
        [Category("Integration")]
        [Category("AzureDevOpsAPI")]
        public async Task GetPullRequestIterationsAsync_Should_Return_Iterations_List()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _apiClient.GetPullRequestIterationsAsync(_testPullRequestId);

            // Assert
            Assert.That(result, Is.Not.Null, "Iterations should not be null");
            Assert.That(result, Is.Not.Empty, "Iterations should not be empty");
            Assert.That(result, Does.Contain("value"), "Response should contain 'value' array");
            Assert.That(result, Does.Contain("id"), "Response should contain iteration 'id' field");
        }

        [Test]
        [Category("Integration")]
        [Category("AzureDevOpsAPI")]
        public async Task GetPullRequestIterationChangesAsync_Should_Return_Changes_For_Iteration()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");
            
            // First get iterations to find a valid iteration ID
            var iterations = await _apiClient.GetPullRequestIterationsAsync(_testPullRequestId);
            var iterationIdMatch = System.Text.RegularExpressions.Regex.Match(iterations, @"""id"":\s*(\d+)");
            Assert.That(iterationIdMatch.Success, Is.True, "Should find at least one iteration ID");
            var iterationId = int.Parse(iterationIdMatch.Groups[1].Value);

            // Act
            var result = await _apiClient.GetPullRequestIterationChangesAsync(_testPullRequestId, iterationId);

            // Assert
            Assert.That(result, Is.Not.Null, "Changes should not be null");
            Assert.That(result, Is.Not.Empty, "Changes should not be empty");
            Assert.That(result, Does.Contain("changeEntries"), "Response should contain 'changeEntries' array");
        }

        [Test]
        [Category("Integration")]
        [Category("AzureDevOpsAPI")]
        public async Task GetPullRequestDiffAsync_Should_Return_Changes_From_Last_Iteration()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _apiClient.GetPullRequestDiffAsync(_testPullRequestId);

            // Assert
            Assert.That(result, Is.Not.Null, "Diff should not be null");
            Assert.That(result, Is.Not.Empty, "Diff should not be empty");
            
            // Verify it contains actual change data
            Assert.That(result, Does.Contain("changeEntries"), "Should contain changeEntries");
            Assert.That(result, Does.Contain("changeType"), "Should contain changeType (add/edit/delete)");
            Assert.That(result, Does.Contain("path"), "Should contain file path");
            Assert.That(result, Does.Contain("objectId"), "Should contain object ID (git hash)");
        }
    }
}
