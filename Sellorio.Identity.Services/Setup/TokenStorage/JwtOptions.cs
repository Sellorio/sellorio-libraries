using Sellorio.Identity.Data;

namespace Sellorio.Identity.Services.Setup.TokenStorage;

public record JwtOptions<TUser>(
    string Issuer,
    string Audience,
    string SigningKey)
        : TokenStorageOptions<TUser>
    where TUser : UserBase;
