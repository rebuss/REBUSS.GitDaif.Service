namespace REBUSS.GitDaif.Service.AzureDevOpsAPI.Models
{
    public class PullRequestDiffResult
    {
        public int PullRequestId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
