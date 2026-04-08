using Microsoft.Extensions.DependencyInjection;
using Sellorio.Blazor.Components.Services;

namespace Sellorio.Blazor.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAoServices(this IServiceCollection services)
    {
        return services.AddScoped<IResultPopupService, ResultPopupService>();
    }
}
