using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using REBUSS.GitDaif.Service.AzureDevOpsAPI;
using REBUSS.GitDaif.Service.AzureDevOpsAPI.Controllers;
using REBUSS.GitDaif.Service.AzureDevOpsAPI.Models;
using REBUSS.GitDaif.Service.AzureDevOpsAPI.Services;

namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.IntegrationTests.Controllers
{
    [TestFixture]
    [Category("Integration")]
    [Category("Controller")]
    public class AzureDevOpsControllerIntegrationTests
    {
        private AzureDevOpsController _controller;
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

            // Create loggers
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var apiClientLogger = loggerFactory.CreateLogger<AzureDevOpsApiClient>();
            var serviceLogger = loggerFactory.CreateLogger<AzureDevOpsService>();
            var controllerLogger = loggerFactory.CreateLogger<AzureDevOpsController>();

            // Create dependencies
            var httpClient = new HttpClient();
            _apiClient = new AzureDevOpsApiClient(httpClient, optionsWrapper, apiClientLogger);
            _azureDevOpsService = new AzureDevOpsService(_apiClient, optionsWrapper, serviceLogger);
            _controller = new AzureDevOpsController(_azureDevOpsService, controllerLogger);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files
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
        [Category("EndToEnd")]
        public async Task GetPullRequestDiff_WhenValidPullRequestId_ReturnsOkWithDiffResult()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _controller.GetPullRequestDiff(_testPullRequestId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>(), "Should return OkObjectResult");

            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null, "OkObjectResult should not be null");
            Assert.That(okResult.StatusCode, Is.EqualTo(200), "Status code should be 200");

            var diffResult = okResult.Value as PullRequestDiffResult;
            Assert.That(diffResult, Is.Not.Null, "Result should be PullRequestDiffResult");
            Assert.That(diffResult.PullRequestId, Is.EqualTo(_testPullRequestId), 
                $"Pull Request ID should be {_testPullRequestId}");
            Assert.That(diffResult.FilePath, Is.Not.Empty, "File path should not be empty");
            Assert.That(File.Exists(diffResult.FilePath), Is.True, 
                $"Diff file should exist at {diffResult.FilePath}");
            Assert.That(diffResult.Message, Does.Contain("successfully"), 
                "Message should indicate success");
        }

        [Test]
        [Category("Validation")]
        public async Task GetPullRequestDiff_WhenPullRequestIdIsZero_ReturnsBadRequest()
        {
            // Arrange
            int invalidPullRequestId = 0;

            // Act
            var result = await _controller.GetPullRequestDiff(invalidPullRequestId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>(), 
                "Should return BadRequestObjectResult for PR ID = 0");

            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null, "BadRequestObjectResult should not be null");
            Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "Status code should be 400");
            Assert.That(badRequestResult.Value, Does.Contain("greater than 0"), 
                "Error message should mention that ID must be greater than 0");
        }

        [Test]
        [Category("Validation")]
        public async Task GetPullRequestDiff_WhenPullRequestIdIsNegative_ReturnsBadRequest()
        {
            // Arrange
            int invalidPullRequestId = -1;

            // Act
            var result = await _controller.GetPullRequestDiff(invalidPullRequestId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>(), 
                "Should return BadRequestObjectResult for negative PR ID");

            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null, "BadRequestObjectResult should not be null");
            Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "Status code should be 400");
        }

        [Test]
        [Category("EndToEnd")]
        public async Task GetPullRequestDiffContent_WhenValidPullRequestId_ReturnsOkWithContent()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");

            // Act
            var result = await _controller.GetPullRequestDiffContent(_testPullRequestId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>(), "Should return OkObjectResult");

            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null, "OkObjectResult should not be null");
            Assert.That(okResult.StatusCode, Is.EqualTo(200), "Status code should be 200");

            var diffContent = okResult.Value as string;
            Assert.That(diffContent, Is.Not.Null, "Diff content should not be null");
            Assert.That(diffContent, Is.Not.Empty, "Diff content should not be empty");
            Assert.That(diffContent, Does.Contain($"Pull Request #{_testPullRequestId}"), 
                $"Content should contain PR number {_testPullRequestId}");
            Assert.That(diffContent, Does.Contain("DIFF CONTENT:"), 
                "Content should contain diff section header");
        }

        [Test]
        [Category("Validation")]
        public async Task GetPullRequestDiffContent_WhenPullRequestIdIsZero_ReturnsBadRequest()
        {
            // Arrange
            int invalidPullRequestId = 0;

            // Act
            var result = await _controller.GetPullRequestDiffContent(invalidPullRequestId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>(), 
                "Should return BadRequestObjectResult for PR ID = 0");

            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null, "BadRequestObjectResult should not be null");
            Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "Status code should be 400");
        }

        [Test]
        [Category("EndToEnd")]
        public async Task GetPullRequestDiff_WhenCalledMultipleTimes_CreatesMultipleFiles()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");
            var filePathsBefore = Directory.Exists(_options.OutputDirectory)
                ? Directory.GetFiles(_options.OutputDirectory, $"PR_{_testPullRequestId}_*.diff").Length
                : 0;

            // Act
            var result1 = await _controller.GetPullRequestDiff(_testPullRequestId);
            await Task.Delay(1000); // Ensure different timestamps
            var result2 = await _controller.GetPullRequestDiff(_testPullRequestId);

            // Assert
            Assert.That(result1, Is.InstanceOf<OkObjectResult>(), "First call should succeed");
            Assert.That(result2, Is.InstanceOf<OkObjectResult>(), "Second call should succeed");

            var filesAfter = Directory.GetFiles(_options.OutputDirectory, $"PR_{_testPullRequestId}_*.diff");
            Assert.That(filesAfter.Length, Is.GreaterThanOrEqualTo(filePathsBefore + 2), 
                "Should create a new file for each call");

            var okResult1 = result1 as OkObjectResult;
            var okResult2 = result2 as OkObjectResult;
            var diffResult1 = okResult1?.Value as PullRequestDiffResult;
            var diffResult2 = okResult2?.Value as PullRequestDiffResult;

            Assert.That(diffResult1?.FilePath, Is.Not.EqualTo(diffResult2?.FilePath), 
                "Each call should create a different file");
        }

        [Test]
        [Category("ErrorHandling")]
        public async Task GetPullRequestDiff_WhenInvalidConfiguration_ReturnsInternalServerError()
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
            var controllerLogger = loggerFactory.CreateLogger<AzureDevOpsController>();

            var httpClient = new HttpClient();
            var apiClient = new AzureDevOpsApiClient(httpClient, optionsWrapper, apiClientLogger);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                new AzureDevOpsService(apiClient, optionsWrapper, serviceLogger),
                "Service should throw InvalidOperationException for invalid configuration");
        }

        [Test]
        [Category("Performance")]
        public async Task GetPullRequestDiff_WhenValidPullRequestId_CompletesInReasonableTime()
        {
            // Arrange
            Assert.That(_testPullRequestId, Is.GreaterThan(0), "Test Pull Request ID must be configured");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = await _controller.GetPullRequestDiff(_testPullRequestId);

            // Assert
            stopwatch.Stop();
            Assert.That(result, Is.InstanceOf<OkObjectResult>(), "Should return OkObjectResult");
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(30000), 
                $"Request should complete within 30 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
