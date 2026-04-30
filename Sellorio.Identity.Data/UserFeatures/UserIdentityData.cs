using Microsoft.EntityFrameworkCore;

namespace Sellorio.Identity.Data.UserFeatures;

[Owned]
public class UserIdentityData
{
    public UserVerificationData? Verification { get; set; }
    public UserEmailData? Email { get; set; }
    public UserPasswordData? Password { get; set; }
}
