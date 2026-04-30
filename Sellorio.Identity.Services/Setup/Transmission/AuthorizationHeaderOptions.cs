using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.Transmission;

public record AuthorizationHeaderOptions<TUser>(string Scheme = "Bearer") : TransmissionOptions<TUser>
    where TUser : UserBase;