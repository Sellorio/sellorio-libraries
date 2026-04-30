using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public interface IIdentityTokenStorageBuilder<TUser>
    where TUser : UserBase
{
    IIdentityTransmissionBuilder<TUser> WithJwt(string issuer, string audience, string signingKey);
}
