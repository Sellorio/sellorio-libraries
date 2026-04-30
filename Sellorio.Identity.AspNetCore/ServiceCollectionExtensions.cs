using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sellorio.Identity.Data;
using Sellorio.Identity.Services.Setup;
using Sellorio.Identity.Services.Setup.TokenStorage;
using Sellorio.Identity.Services.Setup.Transmission;

namespace Sellorio.Identity.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IIdentityTokenStorageBuilder<TUser> AddSellorioIdentityForAspNetCore<TUser>(this IServiceCollection services)
            where TUser : UserBase
    {
        return IdentityBuilder.Create<TUser>(services, BuildForAspNetCore);
    }

    private static void BuildForAspNetCore<TUser>(IServiceCollection services, IdentityOptions<TUser> options)
        where TUser : UserBase
    {
        services.AddAuthorization();

        if (options.TokenStorageOptions is JwtOptions<TUser> jwtOptions)
        {
            services
                .AddAuthentication(
                    options.TransmissionOptions is AuthorizationHeaderOptions<TUser> authorizationHeaderOptions
                        ? authorizationHeaderOptions.Scheme
                        : JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                        NameClaimType = System.Security.Claims.ClaimTypes.Name,
                        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    switch (options.TransmissionOptions)
                    {
                        case AuthorizationHeaderOptions<TUser> authorizationHeaderOptions:
                        {
                            o.Events = new JwtBearerEvents
                            {
                                OnMessageReceived = context =>
                                {
                                    if (string.IsNullOrWhiteSpace(context.Token) &&
                                        context.Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValue) &&
                                        authorizationHeaderValue.Count == 1 &&
                                        authorizationHeaderValue[0] is string authorizationValue && // null check and alias for readability
                                        authorizationValue.StartsWith(authorizationHeaderOptions.Scheme, StringComparison.OrdinalIgnoreCase))
                                    {
                                        context.Token = authorizationValue.Substring(authorizationHeaderOptions.Scheme.Length + 1);
                                    }

                                    return Task.CompletedTask;
                                }
                            };
                            break;
                        }
                        case CookieOptions<TUser> cookieOptions:
                        {
                            o.Events = new JwtBearerEvents
                            {
                                OnMessageReceived = context =>
                                {
                                    if (string.IsNullOrWhiteSpace(context.Token) &&
                                        context.Request.Cookies.TryGetValue(cookieOptions.CookieName, out var token))
                                    {
                                        context.Token = token;
                                    }

                                    return Task.CompletedTask;
                                }
                            };
                            break;
                        }
                        default:
                            throw new NotSupportedException();
                    }
                });
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
