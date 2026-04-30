using Microsoft.AspNetCore.Builder;

namespace Sellorio.Identity.AspNetCore;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSellorioIdentity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
