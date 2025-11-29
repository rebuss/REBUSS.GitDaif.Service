using Microsoft.AspNetCore.Mvc;
using REBUSS.GitDaif.Service.AzureDevOpsAPI.Services;

namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AzureDevOpsController : ControllerBase
    {
        private readonly AzureDevOpsService _azureDevOpsService;
        private readonly ILogger<AzureDevOpsController> _logger;

        public AzureDevOpsController(AzureDevOpsService azureDevOpsService, ILogger<AzureDevOpsController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _logger = logger;
        }

        /// <summary>
        /// Pobiera diff dla danego Pull Request z Azure DevOps i zapisuje go do pliku
        /// </summary>
        /// <param name="pullRequestId">Numer Pull Request</param>
        /// <returns>Ścieżka do zapisanego pliku diff</returns>
        [HttpGet("pullrequest/{pullRequestId}/diff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPullRequestDiff(int pullRequestId)
        {
            try
            {
                if (pullRequestId <= 0)
                {
                    return BadRequest("Pull Request ID must be greater than 0");
                }

                _logger.LogInformation("Processing request to get diff for Pull Request {PullRequestId}", pullRequestId);

                var result = await _azureDevOpsService.SavePullRequestDiffAsync(pullRequestId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration error while processing Pull Request {PullRequestId}", pullRequestId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Configuration error. Please check application settings.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Azure DevOps API error while processing Pull Request {PullRequestId}", pullRequestId);
                return StatusCode(StatusCodes.Status502BadGateway, 
                    "Error communicating with Azure DevOps API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing Pull Request {PullRequestId}", pullRequestId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Pobiera zawartość diff dla danego Pull Request bez zapisywania do pliku
        /// </summary>
        /// <param name="pullRequestId">Numer Pull Request</param>
        /// <returns>Zawartość diff jako tekst</returns>
        [HttpGet("pullrequest/{pullRequestId}/diff/content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPullRequestDiffContent(int pullRequestId)
        {
            try
            {
                if (pullRequestId <= 0)
                {
                    return BadRequest("Pull Request ID must be greater than 0");
                }

                _logger.LogInformation("Processing request to get diff content for Pull Request {PullRequestId}", pullRequestId);

                var diffContent = await _azureDevOpsService.GetPullRequestDiffContentAsync(pullRequestId);

                return Ok(diffContent);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration error while processing Pull Request {PullRequestId}", pullRequestId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Configuration error. Please check application settings.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Azure DevOps API error while processing Pull Request {PullRequestId}", pullRequestId);
                return StatusCode(StatusCodes.Status502BadGateway, 
                    "Error communicating with Azure DevOps API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing Pull Request {PullRequestId}", pullRequestId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while processing your request.");
            }
        }
    }
}
