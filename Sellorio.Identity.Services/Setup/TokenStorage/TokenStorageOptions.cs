using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.TokenStorage;

public abstract record TokenStorageOptions<TUser>
    where TUser : UserBase;
