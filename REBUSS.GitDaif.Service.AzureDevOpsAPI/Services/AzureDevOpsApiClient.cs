using Microsoft.Extensions.Options;
using System.Text;

namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.Services
{
    public class AzureDevOpsApiClient : IAzureDevOpsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AzureDevOpsOptions _options;
        private readonly ILogger<AzureDevOpsApiClient> _logger;

        public AzureDevOpsApiClient(
            HttpClient httpClient,
            IOptions<AzureDevOpsOptions> options,
            ILogger<AzureDevOpsApiClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            var base64Pat = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Pat);
            _httpClient.BaseAddress = new Uri($"https://dev.azure.com/{_options.OrganizationName}/");
        }

        public async Task<string> GetPullRequestDiffAsync(int pullRequestId)
        {
            try
            {
                _logger.LogInformation("Fetching diff for PR {PullRequestId} using iterations and changes", pullRequestId);
                
                // Get iterations
                var iterations = await GetPullRequestIterationsAsync(pullRequestId);
                
                // Parse iterations to get the last iteration ID
                // Note: This is a simple implementation. In production, you'd want to properly parse JSON
                var lastIterationId = ExtractLastIterationId(iterations);
                
                if (lastIterationId > 0)
                {
                    // Get changes for the last iteration
                    var changes = await GetPullRequestIterationChangesAsync(pullRequestId, lastIterationId);
                    return changes;
                }
                else
                {
                    _logger.LogWarning("No iterations found for PR {PullRequestId}, returning empty diff", pullRequestId);
                    return "{}";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching diff for PR {PullRequestId}", pullRequestId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching diff for PR {PullRequestId}", pullRequestId);
                throw;
            }
        }

        public async Task<string> GetPullRequestIterationsAsync(int pullRequestId)
        {
            try
            {
                var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}/pullRequests/{pullRequestId}/iterations?api-version=7.0";
                
                var fullUrl = new Uri(_httpClient.BaseAddress, url);
                _logger.LogInformation("Fetching iterations for PR {PullRequestId} from: {FullUrl}", pullRequestId, fullUrl);
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Azure DevOps API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
                
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching iterations for PR {PullRequestId}", pullRequestId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching iterations for PR {PullRequestId}", pullRequestId);
                throw;
            }
        }

        public async Task<string> GetPullRequestIterationChangesAsync(int pullRequestId, int iterationId)
        {
            try
            {
                var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}/pullRequests/{pullRequestId}/iterations/{iterationId}/changes?api-version=7.0";
                
                var fullUrl = new Uri(_httpClient.BaseAddress, url);
                _logger.LogInformation("Fetching changes for PR {PullRequestId} iteration {IterationId} from: {FullUrl}", pullRequestId, iterationId, fullUrl);
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Azure DevOps API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
                
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching changes for PR {PullRequestId} iteration {IterationId}", pullRequestId, iterationId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching changes for PR {PullRequestId} iteration {IterationId}", pullRequestId, iterationId);
                throw;
            }
        }

        private int ExtractLastIterationId(string iterationsJson)
        {
            try
            {
                // Simple regex to find the last "id": value
                var matches = System.Text.RegularExpressions.Regex.Matches(iterationsJson, @"""id"":\s*(\d+)");
                if (matches.Count > 0)
                {
                    var lastMatch = matches[matches.Count - 1];
                    return int.Parse(lastMatch.Groups[1].Value);
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing iteration ID from JSON");
                return 0;
            }
        }

        public async Task<string> GetPullRequestDetailsAsync(int pullRequestId)
        {
            try
            {
                var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}/pullRequests/{pullRequestId}?api-version=7.0";
                
                // Log full URL for debugging
                var fullUrl = new Uri(_httpClient.BaseAddress, url);
                _logger.LogInformation("Fetching details for PR {PullRequestId} from full URL: {FullUrl}", pullRequestId, fullUrl);
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Azure DevOps API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
                
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching details for PR {PullRequestId}", pullRequestId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching details for PR {PullRequestId}", pullRequestId);
                throw;
            }
        }
    }
}
