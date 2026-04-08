using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Sellorio.Blazor.Components;

public class AoDialogBase<TResult> : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await MudDialog.SetOptionsAsync(new()
        {
            CloseButton = true,
            CloseOnEscapeKey = true,
            BackdropClick = true
        });
    }

    protected void CancelDialog()
    {
        MudDialog.Cancel();
    }

    protected void CloseDialog(TResult result)
    {
        MudDialog.Close(result);
    }
}
