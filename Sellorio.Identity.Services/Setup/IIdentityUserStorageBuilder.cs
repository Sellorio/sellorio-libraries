using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public interface IIdentityUserStorageBuilder<TUser>
    where TUser : UserBase
{
    IIdentityOptionalFeaturesBuilder<TUser> WithDbContext<TDbContext>()
        where TDbContext : IIdentityDbContext<TUser>;
}
