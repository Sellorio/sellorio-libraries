namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiExample : OpenApiExtensibleObject
    {
        public string Summary { get; set; }
        public string Description { get; set; }
        public object Value { get; set; }
        public string ExternalValue { get; set; }
    }
}
