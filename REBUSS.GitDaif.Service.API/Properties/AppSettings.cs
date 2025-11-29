namespace REBUSS.GitDaif.Service.API.Properties
{
    public class AppSettings
    {
        private string _diffFilesDirectory;

        public string DiffFilesDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_diffFilesDirectory))
                {
                    _diffFilesDirectory = Path.Combine(Path.GetTempPath(), "GitDaif");
                    
                    // Ensure directory exists
                    if (!Directory.Exists(_diffFilesDirectory))
                    {
                        Directory.CreateDirectory(_diffFilesDirectory);
                    }
                }
                return _diffFilesDirectory;
            }
            set
            {
                _diffFilesDirectory = value;
            }
        }

        public string LocalRepoPath { get; set; }

        public string PersonalAccessToken { get; set; }
    }
}