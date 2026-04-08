using System.Threading.Tasks;
using MudBlazor;
using Sellorio.Results;
using Sellorio.Results.Messages;

namespace Sellorio.Blazor.Components.Services;

internal class ResultPopupService(ISnackbar snackbarService) : IResultPopupService
{
    public Task ShowResultAsPopupAsync(IResult result, string? successMessage)
    {
        if (result == null)
        {
            return Task.CompletedTask;
        }

        if (result.Messages.Count == 0 && successMessage != null)
        {
            snackbarService.Add(successMessage, Severity.Success, o =>
            {
                o.VisibleStateDuration = 5000;
            });
        }
        else
        {
            foreach (var message in result.Messages)
            {
                snackbarService.Add(message.Text, ConvertSeverity(message.Severity), o =>
                {
                    o.VisibleStateDuration = 5000;
                });
            }
        }

        return Task.CompletedTask;
    }

    private static Severity ConvertSeverity(ResultMessageSeverity severity)
    {
        return severity switch
        {
            ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound => Severity.Error,
            ResultMessageSeverity.Warning => Severity.Warning,
            ResultMessageSeverity.Information => Severity.Info,
            _ => throw new System.NotSupportedException()
        };
    }
}
