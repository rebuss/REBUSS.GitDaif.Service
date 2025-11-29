# Test Coverage Documentation - REBUSS.GitDaif.Service.API

## Overview
This document provides an overview of the test coverage for the REBUSS.GitDaif.Service.API project, including unit tests, integration tests, and test infrastructure.

## Test Projects

### REBUSS.GitDaif.Service.API.IntegrationTests
**Framework**: NUnit 4.2.2  
**Target**: .NET 9.0  
**Purpose**: Integration and unit tests for the API project

## Test Infrastructure

### Test Fixtures
- **TestFixtureBase**: Base class for all test fixtures providing common setup/teardown and helper methods
  - Configuration management
  - Temporary directory creation/cleanup
  - Logger creation helpers
  - Options pattern helpers

### Mock Implementations
- **MockAIAgent**: Mock implementation of `IAIAgent` for testing without actual AI calls
  - Configurable response messages
  - Configurable success/failure scenarios
  - Synchronous execution for predictable testing

## Test Coverage by Component

### 1. Controllers

#### PullRequestController (PullRequestControllerTests)
**Location**: `Controllers/PullRequestControllerTests.cs`  
**Test Categories**: Integration, Controller, Validation, ErrorHandling

**Test Coverage**:
- ? Validation scenarios
  - Invalid/null PullRequestData
  - Invalid/missing FileReviewData
  - Invalid LocalFileReviewData
  - Non-existent files
- ? Error handling
  - Invalid repository paths
  - Exception handling in endpoints
- ? File naming and formatting
- ? Controller instantiation

**Test Count**: 11 tests

**Endpoints Tested**:
- `POST /PullRequest/GetDiffFile` - ? Validation
- `POST /PullRequest/Summarize` - ? Validation
- `POST /PullRequest/Review` - ? Validation
- `POST /PullRequest/SummarizeLocalChanges` - ? Basic
- `POST /PullRequest/ReviewLocalChanges` - ? Basic
- `POST /PullRequest/ReviewSingleFile` - ? Validation
- `POST /PullRequest/ReviewSingleLocalFile` - ? Validation

**Notes**:
- End-to-end tests requiring Azure DevOps are marked with `[Ignore]` attribute
- Mock AI agent used to avoid external dependencies

### 2. Services

#### GitService (GitServiceTests)
**Location**: `Git/GitServiceTests.cs`  
**Test Categories**: Integration, GitService, Unit

**Test Coverage**:
- ? Branch name extraction from refs
- ? File path preparation (leading slash removal)
- ? Modified file name extraction from diff
- ? Multiple file detection in diffs
- ? Local changes diff generation
- ?? Pull request operations (requires Azure DevOps - marked as `[Ignore]`)

**Test Count**: 10+ tests

**Methods Tested**:
- `ExtractBranchNameFromRef()` - ? Full coverage
- `PrepareFilePath()` - ? Full coverage (null, with/without slash)
- `ExtractModifiedFileName()` - ? Full coverage
- `IsDiffFileContainsChangesInMultipleFiles()` - ? Full coverage
- `GetLocalChangesDiffContent()` - ? Basic coverage
- `GetPullRequestDiffContent()` - ?? Requires Azure DevOps
- `GetFullDiffFileFor()` - ?? Requires Azure DevOps

#### DiffFileCleanerBackgroundService (DiffFileCleanerBackgroundServiceTests)
**Location**: `Services/DiffFileCleanerBackgroundServiceTests.cs`  
**Test Categories**: Unit, BackgroundService

**Test Coverage**:
- ? Constructor validation (null checks)
- ? File cleanup logic
  - Old files deletion
  - New files retention
  - File extension filtering (.diff.txt only)
  - Date-based cleanup (files older than today)
- ? Error handling
  - Non-existent directories
  - Graceful shutdown
- ? Service lifecycle (Start/Stop)

**Test Count**: 7 tests

### 3. Validators

#### RequestValidator (RequestValidatorTests)
**Location**: `Validators/RequestValidatorTests.cs`  
**Test Categories**: Unit, Validation

**Test Coverage**:

##### PullRequestData Validation
- ? Valid data returns true
- ? Null data returns false
- ? Empty/whitespace organization name
- ? Empty project name
- ? Empty repository name
- ? Zero ID
- ? Negative ID

##### FileReviewData Validation
- ? Valid data returns true
- ? Null data returns false
- ? Invalid base PullRequestData
- ? Empty/whitespace file path

##### LocalFileReviewData Validation
- ? Valid data with existing file returns true
- ? Null data returns false
- ? Empty/whitespace file path
- ? Non-existent file returns false

**Test Count**: 19 tests

**Coverage**: 100% of validation scenarios

## Test Configuration

### appsettings.test.json
```json
{
  "PersonalAccessToken": "YOUR_PAT_TOKEN_HERE",
  "LocalRepoPath": "C:\\Projects\\REBUSS.GitDaif.Service",
  "DiffFilesDirectory": "",
  "OpenAI": {
    "Key": "test-key",
    "Endpoint": "https://test.openai.azure.com/",
    "Model": "gpt-4"
  },
  "TestSettings": {
    "OrganizationName": "REBUSS",
    "ProjectName": "REBUSS",
    "RepositoryName": "REBUSS",
    "PullRequestId": 1,
    "TestFilePath": "README.md"
  }
}
```

## Test Execution

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Validation"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Test Categories
- **Unit**: Fast, isolated tests with no external dependencies
- **Integration**: Tests that interact with file system or require Git repository
- **Validation**: Tests focused on input validation logic
- **Controller**: Tests for API controllers
- **BackgroundService**: Tests for background services
- **ErrorHandling**: Tests focused on error scenarios

## Coverage Summary

| Component | Lines Covered | Test Count | Status |
|-----------|--------------|------------|---------|
| PullRequestController | ~60% | 11 | ? Good |
| GitService | ~70% | 10+ | ? Good |
| RequestValidator | 100% | 19 | ? Excellent |
| DiffFileCleanerBackgroundService | ~90% | 7 | ? Excellent |
| MockAIAgent | 100% | N/A | ? Complete |

**Overall Coverage**: ~70% (estimated)

## Known Limitations

### Tests Requiring External Services
The following tests require actual Azure DevOps configuration and are marked with `[Ignore]`:
- Pull request diff retrieval
- Pull request details fetching
- Branch operations on remote repositories

### Recommended Test Data Setup
To run the full test suite:
1. Configure `appsettings.test.json` with valid Azure DevOps PAT
2. Ensure `LocalRepoPath` points to a valid Git repository
3. Set up test pull request in Azure DevOps
4. Update `TestSettings:PullRequestId` with valid PR ID

## Future Improvements

### Test Coverage Gaps
1. **AzureOpenAI Agent**: No direct tests (tested via MockAIAgent)
2. **WebApplicationBuilderExtensions**: No tests for DI setup
3. **End-to-End Tests**: Limited full-stack testing
4. **Performance Tests**: No load or stress testing

### Recommended Additions
1. Add load testing for concurrent requests
2. Add tests for OpenAI integration (requires test API key)
3. Add tests for configuration validation
4. Add tests for logging output
5. Add mutation testing to verify test quality
6. Add tests for concurrent file access scenarios
7. Add tests for large diff files (performance)

## Test Maintenance Guidelines

### When Adding New Features
1. Create test file in appropriate category folder
2. Inherit from `TestFixtureBase` for consistency
3. Add appropriate `[Category]` attributes
4. Update this documentation

### Test Naming Convention
```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

### Best Practices
1. ? Use AAA pattern (Arrange, Act, Assert)
2. ? One assertion concept per test
3. ? Descriptive test names
4. ? Clean up resources in TearDown
5. ? Use test categories for organization
6. ? Mock external dependencies
7. ? Use `[Ignore]` for tests requiring manual setup

## Continuous Integration

### Build Pipeline Recommendations
```yaml
- Run unit tests (Category=Unit)
- Run validation tests (Category=Validation)
- Generate coverage report
- Fail build if coverage < 70%
- Run integration tests if credentials available
```

## Test Results Location
Test results are output to:
- `TestResults/` directory
- Coverage reports in `coverage/` directory (if configured)

## Support and Issues
For test-related issues:
1. Check `appsettings.test.json` configuration
2. Verify test data setup
3. Check test category filters
4. Review test output logs

---
**Last Updated**: January 2025  
**Test Framework**: NUnit 4.2.2  
**Target Framework**: .NET 9.0
