using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Sellorio.Identity.Data.UserFeatures;

[Owned]
public class UserEmailData
{
    [StringLength(200), EmailAddress]
    public string? Email { get; set; }

    [StringLength(200), EmailAddress]
    public string? NewEmail { get; set; }

    public bool? IsVerified { get; set; }
}
