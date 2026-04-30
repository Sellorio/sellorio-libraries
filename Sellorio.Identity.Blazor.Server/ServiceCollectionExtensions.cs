using Microsoft.Extensions.DependencyInjection;
using Sellorio.Identity.AspNetCore;
using Sellorio.Identity.Data;
using Sellorio.Identity.Services.Setup;

namespace Sellorio.Identity.Blazor.Server;

public static class ServiceCollectionExtensions
{
    public static IIdentityTokenStorageBuilder<TUser> AddSellorioIdentityForBlazorServer<TUser>(this IServiceCollection services)
        where TUser : UserBase
    {
        services.AddCascadingAuthenticationState();
        return services.AddSellorioIdentityForAspNetCore<TUser>();
    }
}
