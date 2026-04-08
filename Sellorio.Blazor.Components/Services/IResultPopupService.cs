using System.Threading.Tasks;
using Sellorio.Results;

namespace Sellorio.Blazor.Components.Services;

public interface IResultPopupService
{
    Task ShowResultAsPopupAsync(IResult result, string? successMessage);
}
