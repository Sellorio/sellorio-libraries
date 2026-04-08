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

    public event Action? IsDisabledChanged;

    public DisableStateScope(IDisableStateScope? parent, IDialogService dialogService)
    {
        _parent = parent;

        if (_parent != null)
        {
            _parent.IsDisabledChanged += ParentDisableStateChanged;
        }

        _dialogProvider = new(this, dialogService);
    }

    public void UpdateState(bool isDisabled)
    {
        if (_isInnerDisabled != isDisabled)
        {
            _isInnerDisabled = isDisabled;
            IsDisabledChanged?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_parent != null)
        {
            _parent.IsDisabledChanged -= ParentDisableStateChanged;
        }
    }

    private void ParentDisableStateChanged()
    {
        IsDisabledChanged?.Invoke();
    }
}
