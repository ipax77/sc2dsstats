using ChartJs.Blazor.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Shared;
using sc2dsstats.rlib.Services;

namespace sc2dsstats.rlib
{
    public partial class StatsComponent : ComponentBase
    {
        [Inject]
        protected ILogger<StatsComponent> logger { get; set; }

        [Parameter]
        public DsRequest Request { get; set; }

        [Parameter]
        public EventCallback<DsRequest> onOptionsChanged { get; set; }

        [Parameter]
        public IDataService dataService { get; set; }

        [Parameter]
        public bool playerStats { get; set; } = false;

        ChartComponent chartComponent;
        List<string> cmdrList;
        List<string> playerList;
        DsResponse response;
        string Info = String.Empty;
        bool isFirstRendered = false;
        bool isDataAvailable = false;
        bool isLoading = true;
        ConfigBase _config;
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);


        protected override Task OnInitializedAsync()
        {
            cmdrList = DSData.cmdrs.ToList();
            playerList = new List<string>() { "Global", "Uploaders" };
            cmdrList.Insert(0, "ALL");
            if (Request == null)
                Request = new DsRequest("Winrate", "This Year", playerStats);
            LoadData(Request);
            return base.OnInitializedAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender && !isFirstRendered)
            {
                isFirstRendered = true;
                SetChartData();
            }
            base.OnAfterRender(firstRender);
        }

        async Task LoadData(DsRequest Request, bool resetChart = true)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (isLoading == false)
                {
                    isLoading = true;
                    await InvokeAsync(() => StateHasChanged());
                }
                response = await dataService.LoadData(Request);
                if (response != null)
                {
                    Request.Responses.Add(response);
                    isDataAvailable = true;
                    if (resetChart)
                    {
                        SetChartData();
                        if (Request.doReloadSelected && Request.CmdrsSelected != null)
                        {
                            var cmdrsSelected = Request.CmdrsSelected.Where(x => x.Selected && x.Name != Request.Interest);
                            if (cmdrsSelected.Any())
                            {
                                foreach (var cmdr in cmdrsSelected)
                                {
                                    var cmdrrequest = new DsRequest(Request.Mode, Request.Timespan, Request.Player, cmdr.Name);
                                    var cmdrresponse = await dataService.LoadData(cmdrrequest);
                                    ChartService.AddChartDataSet(_config, cmdrrequest, cmdrresponse);
                                    chartComponent.Update();
                                }
                            }
                        }
                    }
                }
                else
                {
                    Info = "Failed loading data :(";
                }

            }
            catch (Exception e)
            {
                logger.LogError($"failed loading data: {e.Message}");
            }
            finally
            {
                semaphoreSlim.Release();
            }
            isLoading = false;
            await InvokeAsync(() => StateHasChanged());
        }

        async void CmdrSelected(string cmdr)
        {
            if (isLoading)
                return;

            if (_config == null)
            {
                Request.Interest = cmdr;
                LoadData(Request);
            }
            else
            {
                ChartService.RemoveChartDataSet(_config, Request, response);
                Request.Interest = cmdr;
                await LoadData(Request, false);
                ChartService.AddChartDataSet(_config, Request, response);
                chartComponent.Update();
            }
            await onOptionsChanged.InvokeAsync(Request);
        }

        async void Cmdr2Selected(KeyValuePair<bool, string> selected)
        {
            if (isLoading)
                return;

            Request.Interest = selected.Value;

            if (selected.Key == true)
            {

                if (_config == null)
                {
                    LoadData(Request, true);
                }
                else
                {
                    await LoadData(Request, false);
                    ChartService.AddChartDataSet(_config, Request, response);
                    chartComponent.Update();
                }
            }
            else
            {
                ChartService.RemoveChartDataSet(_config, Request, response);
                chartComponent.Update();
            }

            await onOptionsChanged.InvokeAsync(Request);
        }

        void ModeSelected(string mode)
        {
            if (isLoading)
                return;
            Request.SetMode(mode);
            LoadData(Request);
            onOptionsChanged.InvokeAsync(Request);
        }

        void TimespanSelected(string timespan)
        {
            if (isLoading)
                return;
            Request.SetTime(timespan);
            LoadData(Request);
            onOptionsChanged.InvokeAsync(Request);
        }

        void PlayerSelected(string player)
        {
            if (player == "Global")
                Request.SetPlayer(false);
            else if (player == "Uploaders")
                Request.SetPlayer(true);
            LoadData(Request);
            onOptionsChanged.InvokeAsync(Request);
        }

        void OptionsSelected()
        {
            LoadData(Request);
            onOptionsChanged.InvokeAsync(Request);
        }



        void ZeroChanged()
        {
            // Request.BeginAtZero = !Request.BeginAtZero;
            chartComponent.BeginAtZero(Request.BeginAtZero, Request.ChartType);
        }

        void SetChartData()
        {
            if (isFirstRendered && isDataAvailable)
            {
                _config = chartComponent.SetChart(Request, response);
            }
        }


    }
}
