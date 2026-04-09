using System;
using MudBlazor;

namespace Sellorio.Blazor.Components.Providers.DisableState;

internal sealed class DisableStateScope : IDisableStateScope
{
    private readonly IDisableStateScope? _parent;
    private readonly DialogProvider _dialogProvider;
    private bool _isInnerDisabled;

    public IDialogProvider DialogProvider => _dialogProvider;

    public bool IsDisabled => _isInnerDisabled || _parent != null && _parent.IsDisabled;

    public event EventHandler? IsDisabledChanged;

    public DisableStateScope(IDisableStateScope? parent, IDialogService dialogService)
    {
        _parent = parent;
        _parent?.IsDisabledChanged += ParentDisableStateChanged;

        _dialogProvider = new(this, dialogService);
    }

    public void UpdateState(bool isDisabled)
    {
        if (_isInnerDisabled != isDisabled)
        {
            _isInnerDisabled = isDisabled;
            IsDisabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _parent?.IsDisabledChanged -= ParentDisableStateChanged;
    }

    private void ParentDisableStateChanged(object? sender, EventArgs e)
    {
        IsDisabledChanged?.Invoke(this, EventArgs.Empty);
    }
}
