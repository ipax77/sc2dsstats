using ElectronNET.API;
using ElectronNET.API.Entities;
using System;
using System.Collections.Generic;
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

            Console.WriteLine("Checking for update ...");
            UpdateCheckResult result;
            try
            {
                Electron.Notification.Show(new NotificationOptions("Hello", await Electron.App.GetVersionAsync()));
                Electron.AutoUpdater.AutoDownload = false;
                result = await Electron.AutoUpdater.CheckForUpdatesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            Electron.Notification.Show(new NotificationOptions("New Version available: ", result.UpdateInfo.Version));

            Console.WriteLine("Update Check running?!");
            return success;
        }
    }
}
