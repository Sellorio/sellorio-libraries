using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Sellorio.Identity.Data.UserFeatures;

[Owned]
public class UserPasswordData
{
    [Required, StringLength(200)]
    public string? LoginHash { get; set; }

    [Required]
    public DateTimeOffset? LastChanged { get; set; }
}
