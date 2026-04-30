using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.OptionalFeatures;

public record OptionalFeaturesOptions<TUser>(bool VerificationEnabled, bool EmailEnabled)
    where TUser : UserBase;
