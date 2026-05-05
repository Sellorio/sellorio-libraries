namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiTag : OpenApiExtensibleObject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
    }
}
