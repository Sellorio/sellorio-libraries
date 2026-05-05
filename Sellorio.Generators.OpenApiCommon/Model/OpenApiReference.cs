namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiReference : OpenApiExtensibleObject
    {
        public string Ref { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
    }
}
