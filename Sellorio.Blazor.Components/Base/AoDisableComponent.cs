using System;
using Microsoft.AspNetCore.Components;
using Sellorio.Blazor.Components.Providers.DisableState;

namespace Sellorio.Blazor.Components.Base;

public abstract class AoDisableComponent : ComponentBase, IDisposable
{
    private bool _isDisposed;

    protected bool DisableContent => DisableStateScope.IsDisabled || Disabled;

    [CascadingParameter]
    public required IDisableStateScope DisableStateScope { private get; init; }

    [Parameter]
    public bool Disabled { private get; set; }

    protected override void OnInitialized()
    {
        if (DisableStateScope != null)
        {
            DisableStateScope.IsDisabledChanged += DisabledChanged;
        }
    }

    private void DisabledChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing && DisableStateScope != null)
            {
                DisableStateScope.IsDisabledChanged -= DisabledChanged;
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
