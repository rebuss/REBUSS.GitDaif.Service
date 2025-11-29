using Microsoft.Extensions.Options;
using REBUSS.GitDaif.Service.AzureDevOpsAPI.Models;

namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.Services
{
    public class AzureDevOpsService
    {
        private readonly IAzureDevOpsApiClient _apiClient;
        private readonly AzureDevOpsOptions _options;
        private readonly ILogger<AzureDevOpsService> _logger;

        public AzureDevOpsService(
            IAzureDevOpsApiClient apiClient,
            IOptions<AzureDevOpsOptions> options,
            ILogger<AzureDevOpsService> logger)
        {
            _apiClient = apiClient;
            _options = options.Value;
            _logger = logger;

            _options.Validate();
        }

        public async Task<string> GetPullRequestDiffContentAsync(int pullRequestId)
        {
            try
            {
                _logger.LogInformation("Fetching diff content for PR {PullRequestId}", pullRequestId);

                var prDetails = await _apiClient.GetPullRequestDetailsAsync(pullRequestId);
                var diffContent = await _apiClient.GetPullRequestDiffAsync(pullRequestId);

                _logger.LogInformation("Successfully retrieved diff for PR {PullRequestId}", pullRequestId);

                return FormatDiffOutput(pullRequestId, prDetails, diffContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting diff for PR {PullRequestId}", pullRequestId);
                throw;
            }
        }

        public async Task<PullRequestDiffResult> SavePullRequestDiffAsync(int pullRequestId)
        {
            try
            {
                var diffContent = await GetPullRequestDiffContentAsync(pullRequestId);

                EnsureOutputDirectoryExists();

                var fileName = GenerateFileName(pullRequestId);
                var filePath = Path.Combine(_options.OutputDirectory, fileName);

                await File.WriteAllTextAsync(filePath, diffContent);
                
                _logger.LogInformation("Saved diff file to: {FilePath}", filePath);

                return new PullRequestDiffResult
                {
                    PullRequestId = pullRequestId,
                    FilePath = filePath,
                    Message = "Diff file saved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving diff for PR {PullRequestId}", pullRequestId);
                throw;
            }
        }

        private void EnsureOutputDirectoryExists()
        {
            if (!Directory.Exists(_options.OutputDirectory))
            {
                Directory.CreateDirectory(_options.OutputDirectory);
                _logger.LogInformation("Created output directory: {OutputDirectory}", _options.OutputDirectory);
            }
        }

        private static string GenerateFileName(int pullRequestId)
        {
            return $"PR_{pullRequestId}_{DateTime.Now:yyyyMMdd_HHmmss}.diff";
        }

        private static string FormatDiffOutput(int pullRequestId, string prDetails, string diffContent)
        {
            return $@"===========================================
Pull Request #{pullRequestId}
===========================================

PR DETAILS:
{prDetails}

===========================================
DIFF CONTENT:
===========================================
{diffContent}
";
        }
    }
}
