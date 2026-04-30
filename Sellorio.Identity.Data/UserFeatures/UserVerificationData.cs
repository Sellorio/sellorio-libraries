using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Sellorio.Identity.Data.UserFeatures;

[Owned]
public class UserVerificationData
{
    [StringLength(40)]
    public string? VerificationCode { get; set; }

    public bool IsVerified { get; set; }

    public DateTimeOffset InitiallyCreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? VerifiedAt { get; set; }
}
