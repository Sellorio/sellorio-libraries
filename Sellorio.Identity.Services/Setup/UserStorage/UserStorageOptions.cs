using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.UserStorage;

public abstract record UserStorageOptions<TUser>
    where TUser : UserBase;
