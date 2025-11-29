namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.Services
{
    public interface IAzureDevOpsApiClient
    {
        Task<string> GetPullRequestDiffAsync(int pullRequestId);
        Task<string> GetPullRequestDetailsAsync(int pullRequestId);
        Task<string> GetPullRequestIterationsAsync(int pullRequestId);
        Task<string> GetPullRequestIterationChangesAsync(int pullRequestId, int iterationId);
    }
}
