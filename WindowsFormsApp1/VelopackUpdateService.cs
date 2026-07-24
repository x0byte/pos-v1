using System;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;

namespace WindowsFormsApp1
{
    public class VelopackUpdateService : IUpdateService
    {
        private readonly UpdateConfiguration configuration;
        private readonly IUpdateSafetyService safetyService;
        private UpdateManager updateManager;
        private UpdateInfo availableUpdate;

        public VelopackUpdateService(UpdateConfiguration configuration, IUpdateSafetyService safetyService)
        {
            this.configuration = configuration;
            this.safetyService = safetyService;
        }

        public string CurrentVersion
        {
            get
            {
                UpdateManager manager = TryGetUpdateManager();
                if (manager != null && manager.CurrentVersion != null)
                {
                    return manager.CurrentVersion.ToString();
                }

                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string FeedUrl
        {
            get { return configuration.FeedUrl; }
        }

        public bool IsFeedConfigured
        {
            get { return configuration.IsConfigured; }
        }

        public async Task<UpdateOperationResult> CheckForUpdatesAsync()
        {
            if (!configuration.IsConfigured)
            {
                return UpdateOperationResult.Fail("Update feed is not configured.");
            }

            try
            {
                UpdateLogger.Info("Update check started");
                UpdateManager manager = GetUpdateManager();
                availableUpdate = await manager.CheckForUpdatesAsync();

                if (availableUpdate == null)
                {
                    UpdateLogger.Info("No update found");
                    return UpdateOperationResult.Ok("No update found.");
                }

                string version = availableUpdate.TargetFullRelease.Version.ToString();
                UpdateLogger.Info("Update available: " + version);
                return UpdateOperationResult.Ok("Update available: " + version, version, true);
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Update check failed", ex);
                return UpdateOperationResult.Fail("Update check failed: " + ex.Message);
            }
        }

        public async Task<UpdateOperationResult> DownloadUpdateAsync(Action<int> progress = null)
        {
            if (!configuration.IsConfigured)
            {
                return UpdateOperationResult.Fail("Update feed is not configured.");
            }

            if (availableUpdate == null)
            {
                UpdateOperationResult checkResult = await CheckForUpdatesAsync();
                if (!checkResult.Success || !checkResult.UpdateAvailable)
                {
                    return checkResult;
                }
            }

            try
            {
                string version = availableUpdate.TargetFullRelease.Version.ToString();
                UpdateLogger.Info("Download started: " + version);
                await GetUpdateManager().DownloadUpdatesAsync(availableUpdate, progress);
                UpdateLogger.Info("Download completed: " + version);
                return UpdateOperationResult.Ok("Download completed.", version, true);
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Download failed", ex);
                return UpdateOperationResult.Fail("Download failed: " + ex.Message);
            }
        }

        public UpdateOperationResult InstallDownloadedUpdateAndRestart()
        {
            if (!configuration.IsConfigured)
            {
                return UpdateOperationResult.Fail("Update feed is not configured.");
            }

            UpdateSafetyResult safety = safetyService.CanInstallUpdate();
            if (!safety.Allowed)
            {
                UpdateLogger.Info("Install blocked with reason: " + safety.Reason);
                return UpdateOperationResult.Fail(safety.Reason);
            }

            try
            {
                VelopackAsset updateToApply = availableUpdate != null
                    ? availableUpdate.TargetFullRelease
                    : GetUpdateManager().UpdatePendingRestart;

                if (updateToApply == null)
                {
                    return UpdateOperationResult.Fail("No downloaded update is ready to install.");
                }

                UpdateLogger.Info("Install started: " + updateToApply.Version);
                GetUpdateManager().ApplyUpdatesAndRestart(updateToApply);
                return UpdateOperationResult.Ok("Install started. The app will restart.");
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Install failed", ex);
                return UpdateOperationResult.Fail("Install failed: " + ex.Message);
            }
        }

        private UpdateManager GetUpdateManager()
        {
            if (updateManager == null)
            {
                updateManager = new UpdateManager(configuration.FeedUrl);
            }

            return updateManager;
        }

        private UpdateManager TryGetUpdateManager()
        {
            if (!configuration.IsConfigured)
            {
                return null;
            }

            try
            {
                return GetUpdateManager();
            }
            catch
            {
                return null;
            }
        }
    }
}
