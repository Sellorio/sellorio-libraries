using Microsoft.EntityFrameworkCore;

namespace Sellorio.Identity.Data;

public interface IIdentityDbContext<TUser>
    where TUser : UserBase
{
    DbSet<TUser> Users { get; set; }
}
