namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiReferenceOr<T>
        where T : class
    {
        public OpenApiReference Reference { get; set; }
        public T Value { get; set; }
    }
}
