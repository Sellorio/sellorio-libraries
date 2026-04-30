using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.Transmission;

public abstract record TransmissionOptions<TUser>
    where TUser : UserBase;
