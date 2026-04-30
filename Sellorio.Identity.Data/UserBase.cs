using Sellorio.Identity.Data.UserFeatures;

namespace Sellorio.Identity.Data;

public abstract class UserBase
{
    public Guid Id { get; set; }

    public UserIdentityData? Identity { get; set; }
}
