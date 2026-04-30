using Microsoft.Extensions.DependencyInjection;
using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public interface IIdentityBuildActor<TUser>
    where TUser : UserBase
{
    void Build(
        IServiceCollection services,
        string issuer,
        string audience,
        string signingKey,
        string cookieName);
}
