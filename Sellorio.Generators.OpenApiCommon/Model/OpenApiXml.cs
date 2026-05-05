namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiXml : OpenApiExtensibleObject
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Prefix { get; set; }
        public bool? Attribute { get; set; }
        public bool? Wrapped { get; set; }
    }
}
