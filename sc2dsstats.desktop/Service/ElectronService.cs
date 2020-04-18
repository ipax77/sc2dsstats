using ElectronNET.API;
using ElectronNET.API.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class ElectronService
    {
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
            bool success = false;

            await Task.Delay(10000);

            Console.WriteLine("Checking for update ...");
            UpdateCheckResult result;


            Electron.AutoUpdater.OnUpdateAvailable += AutoUpdater_OnUpdateAvailable;
            try
            {
                Electron.AutoUpdater.AutoDownload = false;
                Electron.Notification.Show(new NotificationOptions("Hello", await Electron.App.GetVersionAsync()));
                result = await Electron.AutoUpdater.CheckForUpdatesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            finally
            {
                Electron.AutoUpdater.OnUpdateAvailable -= AutoUpdater_OnUpdateAvailable;
            }

            Electron.Notification.Show(new NotificationOptions("New Version available: ", result.UpdateInfo.Version));

            Console.WriteLine("Update Check running?!");
            return success;
        }

        public static async Task<bool> CheckForUpdateAndNotify()
        {
            Console.WriteLine("Checking for update ...");
            UpdateCheckResult result;
            try
            {
                result = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
            Console.WriteLine("Update Check running?!");
            return true;
        }

        private static void OnAction(object obj)
        {
            Console.WriteLine(obj.ToString());
        }

        private static void AutoUpdater_OnUpdateAvailable(UpdateInfo obj)
        {
            Console.WriteLine("Update available! " + obj.Version);
        }

        private static void AutoUpdater_OnDownloadProgress(ProgressInfo obj)
        {
            Console.WriteLine(obj.Percent);
        }
    }
}
