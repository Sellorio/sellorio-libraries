namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiSecurityScheme : OpenApiExtensibleObject
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string In { get; set; }
        public string Scheme { get; set; }
        public string BearerFormat { get; set; }
        public OpenApiOAuthFlows Flows { get; set; }
        public string OpenIdConnectUrl { get; set; }
    }
}
