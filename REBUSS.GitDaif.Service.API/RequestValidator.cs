using REBUSS.GitDaif.Service.API.DTO.Requests;

namespace REBUSS.GitDaif.Service.API.Validators
{
    public static class RequestValidator
    {
        public static bool IsValid(PullRequestData data)
        {
            if (data == null)
                return false;

            return !string.IsNullOrWhiteSpace(data.OrganizationName) &&
                   !string.IsNullOrWhiteSpace(data.RepositoryName) &&
                   !string.IsNullOrWhiteSpace(data.ProjectName) &&
                   data.Id > 0;
        }

        public static bool IsValid(FileReviewData data)
        {
            if (data == null)
                return false;

            return IsValid((PullRequestData)data) &&
                   !string.IsNullOrWhiteSpace(data.FilePath);
        }

        public static bool IsValid(LocalFileReviewData data)
        {
            if (data == null)
                return false;

            return !string.IsNullOrWhiteSpace(data.FilePath) && 
                   File.Exists(data.FilePath);
        }
    }
}
