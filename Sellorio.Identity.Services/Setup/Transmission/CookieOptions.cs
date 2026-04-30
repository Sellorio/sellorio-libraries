using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.Transmission;

public record CookieOptions<TUser>(string CookieName, bool HttpOnly) : TransmissionOptions<TUser>
    where TUser : UserBase;
