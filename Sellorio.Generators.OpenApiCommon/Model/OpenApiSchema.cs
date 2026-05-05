using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiSchema : OpenApiExtensibleObject
    {
        public string Ref { get; set; }
        public string Schema { get; set; }
        public string DynamicRef { get; set; }
        public IDictionary<string, bool> Vocabulary { get; set; } = new Dictionary<string, bool>();
        public string Id { get; set; }
        public string Anchor { get; set; }
        public string DynamicAnchor { get; set; }
        public IDictionary<string, OpenApiSchema> Defs { get; set; } = new Dictionary<string, OpenApiSchema>();
        public string Comment { get; set; }
        public object Type { get; set; }
        public IList<object> Enum { get; set; } = new List<object>();
        public object Const { get; set; }
        public decimal? MultipleOf { get; set; }
        public decimal? Maximum { get; set; }
        public decimal? ExclusiveMaximum { get; set; }
        public decimal? Minimum { get; set; }
        public decimal? ExclusiveMinimum { get; set; }
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
        public string Pattern { get; set; }
        public OpenApiSchema Items { get; set; }
        public IList<OpenApiSchema> PrefixItems { get; set; } = new List<OpenApiSchema>();
        public OpenApiSchema Contains { get; set; }
        public int? MaxContains { get; set; }
        public int? MinContains { get; set; }
        public int? MaxItems { get; set; }
        public int? MinItems { get; set; }
        public bool? UniqueItems { get; set; }
        public int? MaxProperties { get; set; }
        public int? MinProperties { get; set; }
        public IList<string> Required { get; set; } = new List<string>();
        public IDictionary<string, IList<string>> DependentRequired { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, OpenApiSchema> Properties { get; set; } = new Dictionary<string, OpenApiSchema>();
        public IDictionary<string, OpenApiSchema> PatternProperties { get; set; } = new Dictionary<string, OpenApiSchema>();
        public OpenApiBooleanOrSchema AdditionalProperties { get; set; }
        public OpenApiSchema PropertyNames { get; set; }
        public OpenApiBooleanOrSchema UnevaluatedItems { get; set; }
        public OpenApiBooleanOrSchema UnevaluatedProperties { get; set; }
        public IList<OpenApiSchema> AllOf { get; set; } = new List<OpenApiSchema>();
        public IList<OpenApiSchema> AnyOf { get; set; } = new List<OpenApiSchema>();
        public IList<OpenApiSchema> OneOf { get; set; } = new List<OpenApiSchema>();
        public OpenApiSchema Not { get; set; }
        public OpenApiSchema If { get; set; }
        public OpenApiSchema Then { get; set; }
        public OpenApiSchema Else { get; set; }
        public IDictionary<string, OpenApiSchema> DependentSchemas { get; set; } = new Dictionary<string, OpenApiSchema>();
        public string ContentEncoding { get; set; }
        public string ContentMediaType { get; set; }
        public OpenApiSchema ContentSchema { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public object Default { get; set; }
        public bool? Deprecated { get; set; }
        public bool? ReadOnly { get; set; }
        public bool? WriteOnly { get; set; }
        public IList<object> Examples { get; set; } = new List<object>();
        public string Format { get; set; }
        public object Example { get; set; }
        public OpenApiDiscriminator Discriminator { get; set; }
        public OpenApiXml Xml { get; set; }
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
        public IDictionary<string, object> UnrecognizedKeywords { get; set; } = new Dictionary<string, object>();
    }
}
