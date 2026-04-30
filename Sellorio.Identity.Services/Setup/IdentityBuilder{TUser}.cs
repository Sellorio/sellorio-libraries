using Microsoft.Extensions.DependencyInjection;
using Sellorio.Identity.Data;
using Sellorio.Identity.Services.Setup.OptionalFeatures;
using Sellorio.Identity.Services.Setup.TokenStorage;
using Sellorio.Identity.Services.Setup.Transmission;
using Sellorio.Identity.Services.Setup.UserStorage;

namespace Sellorio.Identity.Services.Setup;

internal class IdentityBuilder<TUser>(IServiceCollection services, Action<IServiceCollection, IdentityOptions<TUser>> buildAction) : IIdentityTokenStorageBuilder<TUser>, IIdentityTransmissionBuilder<TUser>, IIdentityUserStorageBuilder<TUser>, IIdentityOptionalFeaturesBuilder<TUser>, IIdentityBuilder<TUser>
    where TUser : UserBase
{
    private TokenStorageOptions<TUser>? _tokenStorageOptions;
    private TransmissionOptions<TUser>? _transmissionOptions;
    private UserStorageOptions<TUser>? _userStorageOptions;
    private OptionalFeaturesOptions<TUser>? _optionalFeaturesOptions;

    public IServiceCollection Build()
    {
        var identityOptions = new IdentityOptions<TUser>(_tokenStorageOptions!, _transmissionOptions!, _userStorageOptions!, _optionalFeaturesOptions!);

        services.AddSingleton(identityOptions);

        if (_tokenStorageOptions is JwtOptions<TUser>)
        {
            // no additional work required here, JWT is configured per-framework
        }
        else
        {
            throw new NotSupportedException();
        }

        if (_transmissionOptions is AuthorizationHeaderOptions<TUser> or CookieOptions<TUser>)
        {
            // no additional work required here, transmission is configured per-framework
        }
        else
        {
            throw new NotSupportedException();
        }

        if (_userStorageOptions is DbContextOptions<TUser> dbContextOptions)
        {
            services.AddScoped(sp => (IIdentityDbContext<TUser>)sp.GetRequiredService(dbContextOptions.DbContextType));
        }
        else
        {
            throw new NotSupportedException();
        }

        // add user, email, etc services

        buildAction.Invoke(services, identityOptions);

        return services;
    }

    public IIdentityUserStorageBuilder<TUser> WithAuthorizationHeader(string scheme = "Bearer")
    {
        _transmissionOptions = new AuthorizationHeaderOptions<TUser>(scheme);
        return this;
    }

    public IIdentityUserStorageBuilder<TUser> WithCookie(string cookieName = "auth-token", bool httpOnly = true)
    {
        _transmissionOptions = new CookieOptions<TUser>(cookieName, httpOnly);
        return this;
    }

    public IIdentityOptionalFeaturesBuilder<TUser> WithDbContext<TDbContext>() where TDbContext : IIdentityDbContext<TUser>
    {
        _userStorageOptions = new DbContextOptions<TUser>(typeof(TDbContext));
        return this;
    }

    public IIdentityTransmissionBuilder<TUser> WithJwt(string issuer, string audience, string signingKey)
    {
        _tokenStorageOptions = new JwtOptions<TUser>(issuer, audience, signingKey);
        return this;
    }

    public IIdentityBuilder<TUser> WithNoOptionalFeatures()
    {
        return WithOptionalFeatures([]);
    }

    public IIdentityBuilder<TUser> WithOptionalFeatures(params IEnumerable<IdentityOptionalFeature> features)
    {
        var verificationEnabled = false;
        var emailEnabled = false;

        foreach (var feature in features)
        {
            switch (feature)
            {
                case IdentityOptionalFeature.Verification:
                    verificationEnabled = true;
                    break;
                case IdentityOptionalFeature.Email:
                    emailEnabled = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        _optionalFeaturesOptions = new OptionalFeaturesOptions<TUser>(verificationEnabled, emailEnabled);
        return this;
    }
}
