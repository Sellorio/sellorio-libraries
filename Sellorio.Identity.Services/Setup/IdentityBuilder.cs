using Microsoft.Extensions.DependencyInjection;
using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public static class IdentityBuilder
{
    public static IIdentityTokenStorageBuilder<TUser> Create<TUser>(
        IServiceCollection services,
        Action<IServiceCollection, IdentityOptions<TUser>> buildAction)
            where TUser : UserBase
    {
        return new IdentityBuilder<TUser>(services, buildAction);
    }
}
