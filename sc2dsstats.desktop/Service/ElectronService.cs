using ElectronNET.API;
using ElectronNET.API.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class ElectronService
    {
        public static Version CurrentVersion { get; private set; } = new Version(2, 0, 0);
        public static Version AvailableVersion { get; private set; } = new Version(2, 0, 0);

        public static async Task Resize()
        {
            bool isResized = false;
            BrowserWindow browserWindow = null;
            int failsafe = 32;
            do
            {
                await Task.Delay(125);
                browserWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                if (browserWindow != null)
                {
                    try
                    {
                        lock (browserWindow)
                        {
                            browserWindow.SetPosition(0, 0);
                            browserWindow.SetSize(1920, 1024);
                            browserWindow.SetMenuBarVisibility(false);
                        }
                        isResized = true;
                    }
                    catch
                    {

                    }
                }
                Console.WriteLine(failsafe);
                failsafe--;
            } while (isResized == false && failsafe > 0);
        }

        public static async Task<bool> CheckForUpdate()
        {
            Console.WriteLine("Checking for updates");
            if (HybridSupport.IsElectronActive)
            {
                Console.WriteLine("indahouse");
                try
                {
                    Electron.AutoUpdater.OnError += (message) => Electron.Dialog.ShowErrorBox("Error", message);
                    CurrentVersion = new Version(await Electron.App.GetVersionAsync());
                    // Electron.AutoUpdater.AutoDownload = false;
                    // var updateResult = await Electron.AutoUpdater.CheckForUpdatesAsync();
                    var updateResult = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
                    AvailableVersion = new Version(updateResult.UpdateInfo.Version);
                    Console.WriteLine($"Got Version {AvailableVersion}");
                    if (AvailableVersion > CurrentVersion)
                        return true;
                    else
                        return false;
                } catch (Exception e)
                {
                    Console.WriteLine($"Failed getting current Version: {e.Message}");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }   

        public static async Task DownloadNewVersion(bool install)
        {
            if (AvailableVersion > CurrentVersion)
            {
                Electron.AutoUpdater.OnUpdateDownloaded += (info) =>
                {
                    if (install)
                    {
                        Electron.AutoUpdater.QuitAndInstall(true, true);
                    }
                };

                if (!install)
                {
                    Electron.AutoUpdater.AutoInstallOnAppQuit = true;
                }
                var downloadResult = await Electron.AutoUpdater.DownloadUpdateAsync();
            }
        }             

    }
}
