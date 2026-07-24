using System;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public interface IUpdateService
    {
        string CurrentVersion { get; }
        string FeedUrl { get; }
        bool IsFeedConfigured { get; }

        Task<UpdateOperationResult> CheckForUpdatesAsync();
        Task<UpdateOperationResult> DownloadUpdateAsync(Action<int> progress = null);
        UpdateOperationResult InstallDownloadedUpdateAndRestart();
    }
}
