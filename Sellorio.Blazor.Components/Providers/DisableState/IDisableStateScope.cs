using System;

namespace Sellorio.Blazor.Components.Providers.DisableState;

public interface IDisableStateScope : IDisposable
{
    IDialogProvider DialogProvider { get; }
    bool IsDisabled { get; }

    event EventHandler IsDisabledChanged;
}