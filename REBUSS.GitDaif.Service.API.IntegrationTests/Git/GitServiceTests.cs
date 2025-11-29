using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using REBUSS.GitDaif.Service.API.DTO.Requests;
using REBUSS.GitDaif.Service.API.IntegrationTests.Fixtures;
using REBUSS.GitDaif.Service.API.Properties;
using REBUSS.GitDaif.Service.API.Services;

namespace REBUSS.GitDaif.Service.API.IntegrationTests.Git
{
    [TestFixture]
    [Category("Integration")]
    [Category("GitService")]
    public class GitServiceTests : TestFixtureBase
    {
        private GitService _gitService;
        private string _filePath;
        private string _branchName;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<GitService>();
            _gitService = new GitService(CreateOptions(AppSettings), logger);

            _branchName = "testing";
            _filePath = Configuration["TestSettings:TestFilePath"] ?? "README.md";
        }

        [Test]
        [Ignore("Requires actual Azure DevOps Pull Request")]
        public async Task GetBranchNameForPullRequest_Should_Return_Correct_BranchName()
        {
            // Act
            var result = await _gitService.GetBranchNameForPullRequest(GetPullRequestData());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        [Ignore("Requires actual Azure DevOps Pull Request")]
        public async Task GetDiffContentForChanges_Should_Return_Valid_Diff()
        {
            // Arrange
            var localRepoPath = AppSettings.LocalRepoPath;

            // Act
            using var repo = new Repository(localRepoPath);
            var result = await _gitService.GetPullRequestDiffContent(GetPullRequestData(), repo);

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ExtractModifiedFileName_Should_Return_Correct_FileName()
        {
            // Arrange
            var sampleDiff = @"diff --git a/src/Services/GitService.cs b/src/Services/GitService.cs
index 123abc..456def 100644
--- a/src/Services/GitService.cs
+++ b/src/Services/GitService.cs";

            // Act
            var result = _gitService.ExtractModifiedFileName(sampleDiff);

            // Assert
            Assert.That(result, Is.EqualTo("GitService.cs"));
        }

        [Test]
        public void ExtractModifiedFileName_WithEmptyDiff_ReturnsEmptyString()
        {
            // Arrange
            var emptyDiff = "";

            // Act
            var result = _gitService.ExtractModifiedFileName(emptyDiff);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        [Ignore("Requires actual Azure DevOps Pull Request")]
        public async Task GetFullDiffFileFor_Should_Return_Valid_Diff()
        {
            // Arrange
            var localRepoPath = AppSettings.LocalRepoPath;

            // Act
            using var repo = new Repository(localRepoPath);
            var result = await _gitService.GetFullDiffFileFor(repo, GetPullRequestData(), _filePath);

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetLocalChangesDiffContent_Should_Return_Valid_Diff_Or_Empty()
        {
            // Act
            var result = await _gitService.GetLocalChangesDiffContent();

            // Assert
            Assert.That(result, Is.Not.Null);
            // Result can be empty if there are no local changes
        }

        [Test]
        public void IsDiffFileContainsChangesInMultipleFiles_WithSingleFile_ReturnsFalse()
        {
            // Arrange
            var singleFileDiff = @"diff --git a/file.cs b/file.cs
index 123..456 100644
--- a/file.cs
+++ b/file.cs";

            // Act
            var result = _gitService.IsDiffFileContainsChangesInMultipleFiles(singleFileDiff);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsDiffFileContainsChangesInMultipleFiles_WithMultipleFiles_ReturnsTrue()
        {
            // Arrange
            var multiFileDiff = @"diff --git a/file1.cs b/file1.cs
index 123..456 100644
--- a/file1.cs
+++ b/file1.cs
diff --git a/file2.cs b/file2.cs
index 789..abc 100644
--- a/file2.cs
+++ b/file2.cs";

            // Act
            var result = _gitService.IsDiffFileContainsChangesInMultipleFiles(multiFileDiff);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void PrepareFilePath_WithLeadingSlash_RemovesSlash()
        {
            // Arrange
            var filePath = "/src/Services/GitService.cs";

            // Act
            var result = _gitService.PrepareFilePath(filePath);

            // Assert
            Assert.That(result, Is.EqualTo("src/Services/GitService.cs"));
        }

        [Test]
        public void PrepareFilePath_WithoutLeadingSlash_ReturnsUnchanged()
        {
            // Arrange
            var filePath = "src/Services/GitService.cs";

            // Act
            var result = _gitService.PrepareFilePath(filePath);

            // Assert
            Assert.That(result, Is.EqualTo(filePath));
        }

        [Test]
        public void PrepareFilePath_WithNull_ReturnsEmpty()
        {
            // Act
            var result = _gitService.PrepareFilePath(null);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ExtractBranchNameFromRef_WithFullRef_ExtractsBranchName()
        {
            // Arrange
            var refName = "refs/heads/feature/my-branch";

            // Act
            var result = _gitService.ExtractBranchNameFromRef(refName);

            // Assert
            Assert.That(result, Is.EqualTo("feature/my-branch"));
        }

        [Test]
        public void ExtractBranchNameFromRef_WithoutRefsPrefix_ReturnsUnchanged()
        {
            // Arrange
            var refName = "main";

            // Act
            var result = _gitService.ExtractBranchNameFromRef(refName);

            // Assert
            Assert.That(result, Is.EqualTo("main"));
        }

        private PullRequestData GetPullRequestData()
        {
            return new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "REBUSS",
                RepositoryName = "REBUSS",
                Id = 1
            };
        }
    }
}