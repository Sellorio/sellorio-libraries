using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.UserStorage;

public record DbContextOptions<TUser>(Type DbContextType) : UserStorageOptions<TUser>
    where TUser : UserBase;