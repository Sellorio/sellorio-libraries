using Sellorio.Identity.Data;
using Sellorio.Identity.Services.Setup.OptionalFeatures;
using Sellorio.Identity.Services.Setup.TokenStorage;
using Sellorio.Identity.Services.Setup.Transmission;
using Sellorio.Identity.Services.Setup.UserStorage;

namespace Sellorio.Identity.Services.Setup;

public record IdentityOptions<TUser>(
    TokenStorageOptions<TUser> TokenStorageOptions,
    TransmissionOptions<TUser> TransmissionOptions,
    UserStorageOptions<TUser> UserStorageOptions,
    OptionalFeaturesOptions<TUser> OptionalFeaturesOptions)
        where TUser : UserBase;
