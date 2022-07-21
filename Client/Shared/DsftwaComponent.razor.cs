using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using sc2dsstats._2022.Shared;

namespace sc2dsstats._2022.Client.Shared
{
    public partial class DsftwaComponent : ComponentBase
    {
        [Inject]
        protected IJSRuntime _js { get; set; }
        [Inject]
        protected NavigationManager _nav { get; set; }
        [Inject]
        protected ILogger<DsftwaComponent> _logger { get; set; }

        [Parameter]
        public string mode { get; set; }
        [Parameter]
        public string guid { get; set; }

        Guid Guid;

        private DsftwaPickbanStatus pickBanStatus;
        private HubConnection hubConnection;

        protected override async Task OnInitializedAsync()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(_nav.ToAbsoluteUri("/pbhub"))
                .Build();

            hubConnection.On<int>("VisitorJoined", (count) =>
            {
                pickBanStatus.Visitors = count;
                StateHasChanged();
            });

            hubConnection.On<int>("VisitorLeft", (count) =>
            {
                pickBanStatus.Visitors = count;
                StateHasChanged();
            });

            hubConnection.On<PickbanLockinfo>("CmdrLocked", (pb) =>
            {
                pickBanStatus.Lock(pb);
                StateHasChanged();
            });

            hubConnection.On<List<PickbanLockinfo>>("ConnectInfo", (pb) =>
            {
                pickBanStatus.Reset();
                foreach (var info in pb)
                {
                    pickBanStatus.Lock(info);
                }
                StateHasChanged();
            });

            await hubConnection.StartAsync();

            _logger.LogInformation($"gernerating guid: {guid ?? "null"}");
            if (!Guid.TryParse(guid, out Guid))
            {
                Guid = Guid.NewGuid();
                _logger.LogInformation($"gernerating guid: {Guid}");
                _nav.NavigateTo($"pickban/{mode}/{Guid}");
                await hubConnection.SendAsync("CreateNewPage", Guid);
            }
            else
            {
                await hubConnection.SendAsync("VisitPage", Guid);
            }
            pickBanStatus = new DsftwaPickbanStatus(Guid);
        }

        private async Task Lock()
        {
            var info = pickBanStatus.Lock();
            await hubConnection.SendAsync("LockBan", info);
        }

        async void CopyClipboard()
        {
            await _js.InvokeVoidAsync("copyClipboard");
        }
    }
}
