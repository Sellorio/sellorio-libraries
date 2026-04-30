using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup;

public interface IIdentityTransmissionBuilder<TUser>
    where TUser : UserBase
{
    IIdentityUserStorageBuilder<TUser> WithAuthorizationHeader(string scheme = "Bearer");
    IIdentityUserStorageBuilder<TUser> WithCookie(string cookieName = "auth-token", bool httpOnly = true);
}
