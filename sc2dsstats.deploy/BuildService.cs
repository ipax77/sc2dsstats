using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class BuildService
{
    public static bool Build()
    {
        FixChartJsBlazorMap();
        int exitCode;
        using (System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
        {
            pProcess.StartInfo.WorkingDirectory = "../sc2dsstats.app";
            pProcess.StartInfo.FileName = "electronize";
            pProcess.StartInfo.Arguments = "build /target win /package-json ./package.json /electron-params --publish=always /p:PublishSingleFile=false";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.RedirectStandardError = true;
            pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.OutputDataReceived += (sender, args) => { Program.logger?.LogInformation(args.Data); };
            pProcess.ErrorDataReceived += (sender, args) => { Program.logger?.LogError(args.Data); };
            pProcess.Start();
            pProcess.BeginOutputReadLine();
            pProcess.BeginErrorReadLine();
            // string output = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();
            pProcess.CancelOutputRead();
            pProcess.CancelErrorRead();
            exitCode = pProcess.ExitCode;
        }
        if (exitCode == 0)
        {
            Program.logger?.LogInformation($"Electronize finished with ExitCode {exitCode}");
            return true;
        }
        else
        {
            Program.logger?.LogError($"Electronize failed with ExitCode {exitCode}");
            return false;
        }
    }

    private static void FixChartJsBlazorMap()
    {
        var mapfile = @"C:\Users\pax77\source\git\ChartJs.Blazor\ChartJsBlazorInterop.js.map";
        var destMapfile = @"C:\Users\pax77\source\git\ChartJs.Blazor\src\ChartJs.Blazor\wwwroot\ChartJsBlazorInterop.js.map";
        if (File.Exists(mapfile) && !File.Exists(destMapfile))
        {
            File.Copy(mapfile, destMapfile);
            Program.logger?.LogInformation("Fixed missing ChartJsBlazorInterop.js.map file.");
        }
    }
}
