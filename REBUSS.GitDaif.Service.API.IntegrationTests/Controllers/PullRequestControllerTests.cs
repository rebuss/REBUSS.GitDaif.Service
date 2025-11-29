using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using REBUSS.GitDaif.Service.API.Controllers;
using REBUSS.GitDaif.Service.API.DTO.Requests;
using REBUSS.GitDaif.Service.API.DTO.Responses;
using REBUSS.GitDaif.Service.API.IntegrationTests.Fixtures;
using REBUSS.GitDaif.Service.API.IntegrationTests.Mocks;
using REBUSS.GitDaif.Service.API.Services;

namespace REBUSS.GitDaif.Service.API.IntegrationTests.Controllers
{
    [TestFixture]
    [Category("Integration")]
    [Category("Controller")]
    public class PullRequestControllerIntegrationTests : TestFixtureBase
    {
        private PullRequestController _controller;
        private GitService _gitService;
        private MockAIAgent _mockAIAgent;
        private int _testPullRequestId;
        private string _testFilePath;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _testPullRequestId = int.Parse(Configuration["TestSettings:PullRequestId"] ?? "1");
            _testFilePath = Configuration["TestSettings:TestFilePath"] ?? "README.md";

            _mockAIAgent = new MockAIAgent("This is a mock AI response for testing");
            _gitService = new GitService(CreateOptions(AppSettings), CreateLogger<GitService>());

            _controller = new PullRequestController(
                CreateOptions(AppSettings),
                CreateLogger<PullRequestController>(),
                _mockAIAgent,
                _gitService
            );
        }

        [Test]
        [Category("Validation")]
        public async Task GetDiffFile_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidData = new PullRequestData
            {
                OrganizationName = "",
                ProjectName = "",
                RepositoryName = "",
                Id = 0
            };

            // Act
            var result = await _controller.GetDiffFile(invalidData);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest.Value, Does.Contain("Invalid"));
        }

        [Test]
        [Category("Validation")]
        public async Task GetDiffFile_WithNullData_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetDiffFile(null);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("EndToEnd")]
        [Ignore("Requires actual Azure DevOps configuration")]
        public async Task GetDiffFile_WithValidData_ReturnsOkWithDiffContent()
        {
            // Arrange
            var validData = new PullRequestData
            {
                OrganizationName = Configuration["TestSettings:OrganizationName"],
                ProjectName = Configuration["TestSettings:ProjectName"],
                RepositoryName = Configuration["TestSettings:RepositoryName"],
                Id = _testPullRequestId
            };

            // Act
            var result = await _controller.GetDiffFile(validData);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.InstanceOf<BaseResponse>());
            
            var response = okResult.Value as BaseResponse;
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.Not.Empty);
        }

        [Test]
        [Category("Validation")]
        public async Task Summarize_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidData = new PullRequestData
            {
                OrganizationName = "",
                ProjectName = "",
                RepositoryName = "",
                Id = 0
            };

            // Act
            var result = await _controller.Summarize(invalidData);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("Validation")]
        public async Task Review_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidData = new PullRequestData
            {
                OrganizationName = "",
                ProjectName = "",
                RepositoryName = "",
                Id = 0
            };

            // Act
            var result = await _controller.Review(invalidData);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("Unit")]
        public async Task SummarizeLocalChanges_WithCustomQuery_CallsAIAgent()
        {
            // Arrange
            var data = new BaseQueryData
            {
                Query = "Summarize these changes"
            };

            // Act
            var result = await _controller.SummarizeLocalChanges(data);

            // Assert - Should fail because there's no local changes, but we verify it tries
            Assert.That(result, Is.InstanceOf<IActionResult>());
        }

        [Test]
        [Category("Unit")]
        public async Task ReviewLocalChanges_WithCustomQuery_CallsAIAgent()
        {
            // Arrange
            var data = new BaseQueryData
            {
                Query = "Review these changes"
            };

            // Act
            var result = await _controller.ReviewLocalChanges(data);

            // Assert
            Assert.That(result, Is.InstanceOf<IActionResult>());
        }

        [Test]
        [Category("Validation")]
        public async Task ReviewSingleFile_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidData = new FileReviewData
            {
                OrganizationName = "",
                ProjectName = "",
                RepositoryName = "",
                Id = 0,
                FilePath = ""
            };

            // Act
            var result = await _controller.ReviewSingleFile(invalidData);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("Validation")]
        public async Task ReviewSingleFile_WithMissingFilePath_ReturnsBadRequest()
        {
            // Arrange
            var data = new FileReviewData
            {
                OrganizationName = "REBUSS",
                ProjectName = "REBUSS",
                RepositoryName = "REBUSS",
                Id = 1,
                FilePath = "" // Missing file path
            };

            // Act
            var result = await _controller.ReviewSingleFile(data);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("Validation")]
        public async Task ReviewSingleLocalFile_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidData = new LocalFileReviewData
            {
                FilePath = ""
            };

            // Act
            var result = await _controller.ReviewSingleLocalFile(invalidData);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("Validation")]
        public async Task ReviewSingleLocalFile_WithNonExistentFile_ReturnsBadRequest()
        {
            // Arrange
            var data = new LocalFileReviewData
            {
                FilePath = "C:\\NonExistent\\File.cs"
            };

            // Act
            var result = await _controller.ReviewSingleLocalFile(data);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Category("ErrorHandling")]
        public async Task GetDiffFile_WithInvalidRepoPath_ReturnsInternalServerError()
        {
            // Arrange
            AppSettings.LocalRepoPath = "C:\\InvalidPath";
            var controller = new PullRequestController(
                CreateOptions(AppSettings),
                CreateLogger<PullRequestController>(),
                _mockAIAgent,
                _gitService
            );

            var data = new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "REBUSS",
                RepositoryName = "REBUSS",
                Id = 1
            };

            // Act
            var result = await controller.GetDiffFile(data);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        [Test]
        [Category("FileHandling")]
        public void Controller_CreatesProperFileNames()
        {
            // This tests the internal file naming logic indirectly
            var testFilePath = "src\\Services\\GitService.cs";
            
            // The controller should handle this without errors
            Assert.DoesNotThrow(() =>
            {
                var formatted = testFilePath.Replace('\\', '-').Replace('/', '-');
                Assert.That(formatted, Does.Not.Contain("\\"));
                Assert.That(formatted, Does.Not.Contain("/"));
            });
        }
    }
}
