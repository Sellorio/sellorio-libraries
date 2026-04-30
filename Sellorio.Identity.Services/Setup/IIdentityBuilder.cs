using Microsoft.Extensions.DependencyInjection;
using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public interface IIdentityBuilder<TUser>
    where TUser : UserBase
{
    IServiceCollection Build();
}
