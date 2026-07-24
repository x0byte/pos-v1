namespace WindowsFormsApp1
{
    public class UpdateOperationResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public string LatestVersion { get; private set; }
        public bool UpdateAvailable { get; private set; }

        public static UpdateOperationResult Ok(string message, string latestVersion = null, bool updateAvailable = false)
        {
            return new UpdateOperationResult
            {
                Success = true,
                Message = message,
                LatestVersion = latestVersion,
                UpdateAvailable = updateAvailable
            };
        }

        public static UpdateOperationResult Fail(string message)
        {
            return new UpdateOperationResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
