using REBUSS.GitDaif.Service.API.DTO.Requests;
using REBUSS.GitDaif.Service.API.Validators;

namespace REBUSS.GitDaif.Service.API.IntegrationTests.Validators
{
    [TestFixture]
    [Category("Unit")]
    [Category("Validation")]
    public class RequestValidatorTests
    {
        #region PullRequestData Validation Tests

        [Test]
        public void IsValid_PullRequestData_WithValidData_ReturnsTrue()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValid_PullRequestData_WithNullData_ReturnsFalse()
        {
            // Act
            var result = RequestValidator.IsValid((PullRequestData)null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_PullRequestData_WithEmptyOrganizationName_ReturnsFalse()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_PullRequestData_WithWhitespaceOrganizationName_ReturnsFalse()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "   ",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_PullRequestData_WithEmptyProjectName_ReturnsFalse()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "",
                RepositoryName = "MyRepo",
                Id = 1
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_PullRequestData_WithEmptyRepositoryName_ReturnsFalse()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "",
                Id = 1
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_PullRequestData_WithZeroId_ReturnsFalse()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 0
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_PullRequestData_WithNegativeId_ReturnsFalse()
        {
            // Arrange
            var data = new PullRequestData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = -1
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region FileReviewData Validation Tests

        [Test]
        public void IsValid_FileReviewData_WithValidData_ReturnsTrue()
        {
            // Arrange
            var data = new FileReviewData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1,
                FilePath = "src/Services/GitService.cs"
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValid_FileReviewData_WithNullData_ReturnsFalse()
        {
            // Act
            var result = RequestValidator.IsValid((FileReviewData)null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_FileReviewData_WithInvalidPullRequestData_ReturnsFalse()
        {
            // Arrange
            var data = new FileReviewData
            {
                OrganizationName = "",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1,
                FilePath = "src/Services/GitService.cs"
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_FileReviewData_WithEmptyFilePath_ReturnsFalse()
        {
            // Arrange
            var data = new FileReviewData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1,
                FilePath = ""
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_FileReviewData_WithWhitespaceFilePath_ReturnsFalse()
        {
            // Arrange
            var data = new FileReviewData
            {
                OrganizationName = "REBUSS",
                ProjectName = "MyProject",
                RepositoryName = "MyRepo",
                Id = 1,
                FilePath = "   "
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region LocalFileReviewData Validation Tests

        [Test]
        public void IsValid_LocalFileReviewData_WithValidData_ReturnsTrue()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var data = new LocalFileReviewData
            {
                FilePath = tempFile
            };

            try
            {
                // Act
                var result = RequestValidator.IsValid(data);

                // Assert
                Assert.That(result, Is.True);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void IsValid_LocalFileReviewData_WithNullData_ReturnsFalse()
        {
            // Act
            var result = RequestValidator.IsValid((LocalFileReviewData)null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_LocalFileReviewData_WithEmptyFilePath_ReturnsFalse()
        {
            // Arrange
            var data = new LocalFileReviewData
            {
                FilePath = ""
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_LocalFileReviewData_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var data = new LocalFileReviewData
            {
                FilePath = "C:\\NonExistent\\File.cs"
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_LocalFileReviewData_WithWhitespaceFilePath_ReturnsFalse()
        {
            // Arrange
            var data = new LocalFileReviewData
            {
                FilePath = "   "
            };

            // Act
            var result = RequestValidator.IsValid(data);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion
    }
}
