using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public interface IIdentityOptionalFeaturesBuilder<TUser>
    where TUser : UserBase
{
    IIdentityBuilder<TUser> WithNoOptionalFeatures();
    IIdentityBuilder<TUser> WithOptionalFeatures(params IEnumerable<IdentityOptionalFeature> features);
}
