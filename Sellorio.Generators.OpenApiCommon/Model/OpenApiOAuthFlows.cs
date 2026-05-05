namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiOAuthFlows : OpenApiExtensibleObject
    {
        public OpenApiOAuthFlow Implicit { get; set; }
        public OpenApiOAuthFlow Password { get; set; }
        public OpenApiOAuthFlow ClientCredentials { get; set; }
        public OpenApiOAuthFlow AuthorizationCode { get; set; }
    }
}
