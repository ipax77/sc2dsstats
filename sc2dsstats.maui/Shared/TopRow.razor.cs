﻿using Microsoft.AspNetCore.Components;
using sc2dsstats.maui.Services;

namespace sc2dsstats.maui.Shared;

public partial class TopRow : ComponentBase, IDisposable
{
    [Inject]
    protected DecodeService decodeService { get; set; } = null!;

    [Inject]
    protected NavigationManager navigationManager { get; set; } = null!;

    private DecodeEventArgs? decodeEventArgs;
    private TimeSpan elapsed = TimeSpan.Zero;
    private TimeSpan eta = TimeSpan.Zero;

    // private ReplaysFailedModal? replaysFailedModal;

    protected override void OnInitialized()
    {
        decodeService.DecodeStateChanged += DecodeService_DecodeStateChanged;
        decodeService.ScanStateChanged += DecodeService_ScanStateChanged;
        base.OnInitialized();
    }

    private void DecodeService_ScanStateChanged(object? sender, ScanEventArgs e)
    {
        InvokeAsync(() => StateHasChanged());
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await decodeService.ScanForNewReplays();
            await InvokeAsync(() => StateHasChanged());
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private void DecodeService_DecodeStateChanged(object? sender, DecodeEventArgs e)
    {
        decodeEventArgs = e;

        elapsed = DateTime.UtcNow - e.Start;

        if (!decodeEventArgs.Done)
        {
            if (decodeEventArgs.Decoded < 10)
            {
                if (UserSettingsService.UserSettings.CpuCoresUsedForDecoding > 0)
                    eta = TimeSpan.FromSeconds(decodeEventArgs.Total * 6 / UserSettingsService.UserSettings.CpuCoresUsedForDecoding) - elapsed;
                else
                    eta = TimeSpan.FromSeconds(decodeEventArgs.Total * 6) - elapsed;
            }
            else
            {
                double one = elapsed.TotalSeconds / (double)decodeEventArgs.Decoded;
                eta = TimeSpan.FromSeconds(one * (decodeEventArgs.Total - decodeEventArgs.Decoded));
            }
        }
        else
        {
            eta = TimeSpan.Zero;
        }

        InvokeAsync(() => StateHasChanged());
    }

    private void ShowFailedReplays()
    {
        // replaysFailedModal?.Show(decodeService.GetErrorReplays().ToList());
    }

    public void Dispose()
    {
        decodeService.DecodeStateChanged -= DecodeService_DecodeStateChanged;
        decodeService.ScanStateChanged -= DecodeService_ScanStateChanged;
    }
}