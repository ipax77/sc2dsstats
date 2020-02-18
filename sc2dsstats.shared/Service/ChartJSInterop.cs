using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Service
{
    public static class ChartJSInterop
    {
        public static async Task<string> ChartChanged(IJSRuntime _jsRuntime, string data)
        {
            // The handleTickerChanged JavaScript method is implemented
            // in a JavaScript file, such as 'wwwroot/tickerJsInterop.js'.
            try
            {
                return await _jsRuntime.InvokeAsync<string>("DynChart", data);
            }
            catch (Exception e)
            {
                return String.Empty;
            }
        }

        public static async Task AddDataset(IJSRuntime _jsRuntime, string data, object lockobject)
        {
            lock (lockobject)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("AddDynChart", data);
                }
                catch { }
            }
        }

        public static async Task RemoveDataset(IJSRuntime _jsRuntime, int data, object lockobject)
        {
            lock (lockobject)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("RemoveDynChart", data);
                }
                catch { }
            }
        }

        public static async Task AddData(IJSRuntime _jsRuntime, string label, double winrate, string dcolor, string dimage, object lockobject)
        {
            lock (lockobject)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("AddData", label, winrate, dcolor, dimage);
                }
                catch (Exception e)
                {
                }
            }
        }

        public static async Task ChangeOptions(IJSRuntime _jsRuntime, string chartoptions)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("ChangeOptionsDynChart", chartoptions);
            }
            catch (Exception e)
            {
            }
        }

        public static async Task SortChart(IJSRuntime _jsRuntime, string labels, string winrates, string images, string colors)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("SortChart", labels, winrates, images, colors);
            }
            catch (Exception e)
            {
            }
        }
    }
}
