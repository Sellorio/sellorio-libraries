namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiInfo : OpenApiExtensibleObject
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string TermsOfService { get; set; }
        public OpenApiContact Contact { get; set; }
        public OpenApiLicense License { get; set; }
        public string Version { get; set; }
    }
}
