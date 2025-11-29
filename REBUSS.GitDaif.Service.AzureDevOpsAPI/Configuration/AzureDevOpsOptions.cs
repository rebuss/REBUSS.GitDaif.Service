namespace REBUSS.GitDaif.Service.AzureDevOpsAPI
{
    public class AzureDevOpsOptions
    {
        public const string SectionName = "AzureDevOps";

        public string OrganizationName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string PersonalAccessToken { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OrganizationName))
                throw new InvalidOperationException($"{nameof(OrganizationName)} is required in {SectionName} configuration");

            if (string.IsNullOrWhiteSpace(ProjectName))
                throw new InvalidOperationException($"{nameof(ProjectName)} is required in {SectionName} configuration");

            if (string.IsNullOrWhiteSpace(RepositoryName))
                throw new InvalidOperationException($"{nameof(RepositoryName)} is required in {SectionName} configuration");

            if (string.IsNullOrWhiteSpace(PersonalAccessToken))
                throw new InvalidOperationException($"{nameof(PersonalAccessToken)} is required in {SectionName} configuration");

            if (string.IsNullOrWhiteSpace(OutputDirectory))
                throw new InvalidOperationException($"{nameof(OutputDirectory)} is required in {SectionName} configuration");
        }
    }
}
