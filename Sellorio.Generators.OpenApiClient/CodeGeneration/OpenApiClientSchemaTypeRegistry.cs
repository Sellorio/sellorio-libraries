using System;
using System.Collections.Generic;
using System.Linq;
using Sellorio.Generators.CSharpCommon.CodeGeneration;
using Sellorio.Generators.OpenApiCommon.Model;

namespace Sellorio.Generators.OpenApiClient.CodeGeneration
{
    internal sealed class OpenApiClientSchemaTypeRegistry
    {
        private readonly OpenApiDocument _document;
        private readonly string _accessibility;
        private readonly Dictionary<string, GeneratedTypeDefinition> _typeDefinitions = new Dictionary<string, GeneratedTypeDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _componentTypeNames = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _interfaceNames = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _usedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        public OpenApiClientSchemaTypeRegistry(OpenApiDocument document, string accessibility)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _accessibility = string.IsNullOrWhiteSpace(accessibility) ? "internal" : accessibility;
        }

        public ResolvedSchemaType ResolveSchemaType(OpenApiSchema schema, string preferredName, bool required)
        {
            if (schema == null)
            {
                return new ResolvedSchemaType(GetNullableType("string", required, false), false, false, "string");
            }

            if (!string.IsNullOrWhiteSpace(schema.Ref))
            {
                return ResolveReferencedSchemaType(schema.Ref, preferredName, required);
            }

            if (schema.OneOf.Count == 1)
            {
                return ResolveSchemaType(schema.OneOf[0], preferredName, required);
            }

            if (schema.AnyOf.Count == 1)
            {
                return ResolveSchemaType(schema.AnyOf[0], preferredName, required);
            }

            if (schema.AllOf.Count == 1)
            {
                return ResolveSchemaType(schema.AllOf[0], preferredName, required);
            }

            var type = schema.Type as string;
            switch (type)
            {
                case "string":
                    if (string.Equals(schema.Format, "uuid", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ResolvedSchemaType(GetNullableType("global::System.Guid", required, true), false, true, "global::System.Guid");
                    }

                    if (string.Equals(schema.Format, "date-time", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ResolvedSchemaType(GetNullableType("global::System.DateTimeOffset", required, true), false, true, "global::System.DateTimeOffset");
                    }

                    return new ResolvedSchemaType(GetNullableType("string", required, false), false, false, "string");
                case "integer":
                    return new ResolvedSchemaType(GetNullableType("int", required, true), false, true, "int");
                case "number":
                    return new ResolvedSchemaType(GetNullableType("decimal", required, true), false, true, "decimal");
                case "boolean":
                    return new ResolvedSchemaType(GetNullableType("bool", required, true), false, true, "bool");
                case "array":
                    var itemType = ResolveSchemaType(schema.Items, preferredName + "Item", true);
                    var listTypeName = "global::System.Collections.Generic.IReadOnlyList<" + itemType.NonNullableTypeName + ">";
                    return new ResolvedSchemaType(required ? listTypeName : listTypeName + "?", false, false, listTypeName);
                case "object":
                    return ResolveObjectSchemaType(schema, preferredName, required);
                default:
                    if (schema.Properties.Count > 0 || schema.AllOf.Count > 0)
                    {
                        return ResolveObjectSchemaType(schema, preferredName, required);
                    }

                    return ResolveObjectSchemaType(schema, preferredName, required);
            }
        }

        public PolymorphicSuccessResponseType ResolvePolymorphicSuccessResponseType(OpenApiSchema schema, string preferredName, string interfaceName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            _interfaceNames.Add(interfaceName);

            if (!string.IsNullOrWhiteSpace(schema.Ref))
            {
                var referencedType = ResolveSchemaType(schema, preferredName, true);
                if (_typeDefinitions.TryGetValue(referencedType.NonNullableTypeName, out var referencedDefinition))
                {
                    referencedDefinition.Interfaces.Add(interfaceName);
                    return new PolymorphicSuccessResponseType(referencedType.NonNullableTypeName, referencedType.NonNullableTypeName, false);
                }

                return CreatePrimitiveWrapperType(referencedType.NonNullableTypeName, preferredName, interfaceName);
            }

            var type = schema.Type as string;
            switch (type)
            {
                case "array":
                    return CreateListType(schema, preferredName, interfaceName);
                case "object":
                    var objectType = ResolveSchemaType(schema, preferredName, true);
                    _typeDefinitions[objectType.NonNullableTypeName].Interfaces.Add(interfaceName);
                    return new PolymorphicSuccessResponseType(objectType.NonNullableTypeName, objectType.NonNullableTypeName, false);
                case "string":
                case "integer":
                case "number":
                case "boolean":
                    var primitiveType = ResolveSchemaType(schema, preferredName, true);
                    return CreatePrimitiveWrapperType(primitiveType.NonNullableTypeName, preferredName, interfaceName);
                default:
                    if (schema.Properties.Count > 0 || schema.AllOf.Count > 0)
                    {
                        var generatedType = ResolveSchemaType(schema, preferredName, true);
                        _typeDefinitions[generatedType.NonNullableTypeName].Interfaces.Add(interfaceName);
                        return new PolymorphicSuccessResponseType(generatedType.NonNullableTypeName, generatedType.NonNullableTypeName, false);
                    }

                    var fallbackType = ResolveSchemaType(schema, preferredName, true);
                    return CreatePrimitiveWrapperType(fallbackType.NonNullableTypeName, preferredName, interfaceName);
            }
        }

        public string GenerateTypeDefinitions()
        {
            var builder = new CSharpSourceBuilder();

            foreach (var interfaceName in _interfaceNames.OrderBy(name => name, StringComparer.Ordinal))
            {
                using (builder.BeginBlock(_accessibility + " partial interface " + interfaceName))
                {
                }

                builder.AppendLine();
            }

            foreach (var typeDefinition in _typeDefinitions.Values.OrderBy(definition => definition.Name, StringComparer.Ordinal))
            {
                if (typeDefinition.Kind == GeneratedTypeDefinitionKind.List)
                {
                    var interfaces = typeDefinition.Interfaces.Count == 0
                        ? string.Empty
                        : ", " + string.Join(", ", typeDefinition.Interfaces.OrderBy(name => name, StringComparer.Ordinal));
                    using (builder.BeginBlock(_accessibility + " partial class " + typeDefinition.Name + " : global::System.Collections.Generic.List<" + typeDefinition.ListItemTypeName + ">" + interfaces))
                    {
                    }
                }
                else if (typeDefinition.Kind == GeneratedTypeDefinitionKind.PrimitiveWrapper)
                {
                    var interfaces = typeDefinition.Interfaces.Count == 0
                        ? string.Empty
                        : " : " + string.Join(", ", typeDefinition.Interfaces.OrderBy(name => name, StringComparer.Ordinal));
                    using (builder.BeginBlock(_accessibility + " partial class " + typeDefinition.Name + interfaces))
                    {
                        builder.AppendLine("public " + typeDefinition.PrimitiveValueTypeName + " Value { get; set; }");
                    }
                }
                else
                {
                    var interfaces = typeDefinition.Interfaces.Count == 0
                        ? string.Empty
                        : " : " + string.Join(", ", typeDefinition.Interfaces.OrderBy(name => name, StringComparer.Ordinal));
                    using (builder.BeginBlock(_accessibility + " partial class " + typeDefinition.Name + interfaces))
                    {
                        foreach (var property in typeDefinition.Properties.OrderBy(property => property.Name, StringComparer.Ordinal))
                        {
                            builder.AppendLine("public " + property.TypeName + " " + property.Name + " { get; set; }" + property.Initializer);
                        }
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private ResolvedSchemaType ResolveReferencedSchemaType(string reference, string preferredName, bool required)
        {
            if (!reference.StartsWith("#/components/schemas/", StringComparison.Ordinal))
            {
                return ResolveObjectSchemaType(new OpenApiSchema(), preferredName, required);
            }

            var schemaName = reference.Substring("#/components/schemas/".Length);
            if (_componentTypeNames.TryGetValue(schemaName, out var existingTypeName))
            {
                return CreateResolvedSchemaType(existingTypeName, required, true, false);
            }

            if (_document.Components == null || !_document.Components.Schemas.TryGetValue(schemaName, out var schema))
            {
                return ResolveObjectSchemaType(new OpenApiSchema(), preferredName, required);
            }

            var resolvedType = ResolveComponentSchemaType(schemaName, schema, required);
            return resolvedType;
        }

        private ResolvedSchemaType ResolveComponentSchemaType(string schemaName, OpenApiSchema schema, bool required)
        {
            if (!string.IsNullOrWhiteSpace(schema.Ref))
            {
                return ResolveReferencedSchemaType(schema.Ref, schemaName, required);
            }

            var type = schema.Type as string;
            switch (type)
            {
                case "object":
                    var typeName = EnsureObjectDefinition(schemaName, schema);
                    _componentTypeNames[schemaName] = typeName;
                    return CreateResolvedSchemaType(typeName, required, true, false);
                case "array":
                    var itemType = ResolveSchemaType(schema.Items, schemaName + "Item", true);
                    var listTypeName = "global::System.Collections.Generic.IReadOnlyList<" + itemType.NonNullableTypeName + ">";
                    return new ResolvedSchemaType(required ? listTypeName : listTypeName + "?", false, false, listTypeName);
                case "string":
                case "integer":
                case "number":
                case "boolean":
                    return ResolveSchemaType(schema, schemaName, required);
                default:
                    if (schema.Properties.Count > 0 || schema.AllOf.Count > 0)
                    {
                        var objectTypeName = EnsureObjectDefinition(schemaName, schema);
                        _componentTypeNames[schemaName] = objectTypeName;
                        return CreateResolvedSchemaType(objectTypeName, required, true, false);
                    }

                    var fallbackTypeName = EnsureObjectDefinition(schemaName, schema);
                    _componentTypeNames[schemaName] = fallbackTypeName;
                    return CreateResolvedSchemaType(fallbackTypeName, required, true, false);
            }
        }

        private ResolvedSchemaType ResolveObjectSchemaType(OpenApiSchema schema, string preferredName, bool required)
        {
            var typeName = EnsureObjectDefinition(preferredName, schema);
            return CreateResolvedSchemaType(typeName, required, true, false);
        }

        private string EnsureObjectDefinition(string preferredName, OpenApiSchema schema)
        {
            var normalizedName = ReserveTypeName(preferredName);
            if (_typeDefinitions.TryGetValue(normalizedName, out var existingDefinition))
            {
                if (existingDefinition.Kind == GeneratedTypeDefinitionKind.Object && existingDefinition.Properties.Count == 0)
                {
                    PopulateObjectDefinition(existingDefinition, schema);
                }

                return normalizedName;
            }

            var definition = new GeneratedTypeDefinition(normalizedName, GeneratedTypeDefinitionKind.Object);
            _typeDefinitions.Add(normalizedName, definition);
            PopulateObjectDefinition(definition, schema);
            return normalizedName;
        }

        private void PopulateObjectDefinition(GeneratedTypeDefinition definition, OpenApiSchema schema)
        {
            foreach (var inheritedSchema in schema.AllOf)
            {
                if (string.IsNullOrWhiteSpace(inheritedSchema.Ref))
                {
                    continue;
                }

                var baseType = ResolveSchemaType(inheritedSchema, definition.Name + "Base", true);
                if (_typeDefinitions.TryGetValue(baseType.NonNullableTypeName, out var baseDefinition))
                {
                    foreach (var property in baseDefinition.Properties)
                    {
                        if (definition.Properties.Any(existing => existing.Name == property.Name))
                        {
                            continue;
                        }

                        definition.Properties.Add(property);
                    }
                }
            }

            foreach (var property in schema.Properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var propertyName = CSharpIdentifier.ToPascalCase(property.Key);
                if (definition.Properties.Any(existing => existing.Name == propertyName))
                {
                    continue;
                }

                var propertyRequired = schema.Required.Contains(property.Key);
                var propertyType = ResolveSchemaType(property.Value, definition.Name + propertyName, propertyRequired);
                var initializer = propertyRequired && !propertyType.IsValueType ? " = null!;" : string.Empty;
                definition.Properties.Add(new GeneratedTypeProperty(propertyName, propertyType.TypeName, initializer));
            }
        }

        private PolymorphicSuccessResponseType CreateListType(OpenApiSchema schema, string preferredName, string interfaceName)
        {
            var itemType = ResolveSchemaType(schema.Items, preferredName + "Item", true);
            var typeName = ReserveTypeName(preferredName);
            if (!_typeDefinitions.ContainsKey(typeName))
            {
                _typeDefinitions.Add(typeName, GeneratedTypeDefinition.CreateList(typeName, itemType.NonNullableTypeName, interfaceName));
            }
            else
            {
                _typeDefinitions[typeName].Interfaces.Add(interfaceName);
            }

            return new PolymorphicSuccessResponseType(typeName, typeName, false);
        }

        private PolymorphicSuccessResponseType CreatePrimitiveWrapperType(string primitiveTypeName, string preferredName, string interfaceName)
        {
            var typeName = ReserveTypeName(preferredName);
            if (!_typeDefinitions.ContainsKey(typeName))
            {
                _typeDefinitions.Add(typeName, GeneratedTypeDefinition.CreatePrimitiveWrapper(typeName, primitiveTypeName, interfaceName));
            }
            else
            {
                _typeDefinitions[typeName].Interfaces.Add(interfaceName);
            }

            return new PolymorphicSuccessResponseType(typeName, primitiveTypeName, true);
        }

        private ResolvedSchemaType CreateResolvedSchemaType(string typeName, bool required, bool isSpecificModel, bool isValueType)
        {
            return new ResolvedSchemaType(GetNullableType(typeName, required, isValueType), isSpecificModel, isValueType, typeName);
        }

        private string ReserveTypeName(string preferredName)
        {
            var candidate = CSharpIdentifier.ToPascalCase(preferredName);
            if (_typeDefinitions.ContainsKey(candidate))
            {
                return candidate;
            }

            if (!_interfaceNames.Contains(candidate) && _usedTypeNames.Add(candidate))
            {
                return candidate;
            }

            var suffix = 2;
            var uniqueName = candidate + suffix;
            while (!_usedTypeNames.Add(uniqueName) || _typeDefinitions.ContainsKey(uniqueName) || _interfaceNames.Contains(uniqueName))
            {
                suffix++;
                uniqueName = candidate + suffix;
            }

            return uniqueName;
        }

        private static string GetNullableType(string typeName, bool required, bool isValueType)
        {
            if (required)
            {
                return typeName;
            }

            if (typeName.EndsWith("?", StringComparison.Ordinal))
            {
                return typeName;
            }

            return isValueType ? typeName + "?" : typeName + "?";
        }
    }

    internal sealed class ResolvedSchemaType
    {
        public string TypeName { get; }

        public bool IsSpecificModel { get; }

        public bool IsValueType { get; }

        public string NonNullableTypeName { get; }

        public ResolvedSchemaType(string typeName, bool isSpecificModel, bool isValueType, string nonNullableTypeName)
        {
            TypeName = typeName;
            IsSpecificModel = isSpecificModel;
            IsValueType = isValueType;
            NonNullableTypeName = nonNullableTypeName;
        }
    }

    internal sealed class PolymorphicSuccessResponseType
    {
        public string ConcreteTypeName { get; }

        public string DeserializeTypeName { get; }

        public bool RequiresWrapping { get; }

        public PolymorphicSuccessResponseType(string concreteTypeName, string deserializeTypeName, bool requiresWrapping)
        {
            ConcreteTypeName = concreteTypeName;
            DeserializeTypeName = deserializeTypeName;
            RequiresWrapping = requiresWrapping;
        }
    }

    internal sealed class GeneratedTypeDefinition
    {
        public string Name { get; }

        public GeneratedTypeDefinitionKind Kind { get; }

        public IList<GeneratedTypeProperty> Properties { get; } = new List<GeneratedTypeProperty>();

        public ISet<string> Interfaces { get; } = new HashSet<string>(StringComparer.Ordinal);

        public string ListItemTypeName { get; }

        public string PrimitiveValueTypeName { get; }

        public GeneratedTypeDefinition(string name, GeneratedTypeDefinitionKind kind, string listItemTypeName = null, string primitiveValueTypeName = null)
        {
            Name = name;
            Kind = kind;
            ListItemTypeName = listItemTypeName;
            PrimitiveValueTypeName = primitiveValueTypeName;
        }

        public static GeneratedTypeDefinition CreateList(string name, string listItemTypeName, string interfaceName)
        {
            var definition = new GeneratedTypeDefinition(name, GeneratedTypeDefinitionKind.List, listItemTypeName: listItemTypeName);
            definition.Interfaces.Add(interfaceName);
            return definition;
        }

        public static GeneratedTypeDefinition CreatePrimitiveWrapper(string name, string primitiveValueTypeName, string interfaceName)
        {
            var definition = new GeneratedTypeDefinition(name, GeneratedTypeDefinitionKind.PrimitiveWrapper, primitiveValueTypeName: primitiveValueTypeName);
            definition.Interfaces.Add(interfaceName);
            return definition;
        }
    }

    internal sealed class GeneratedTypeProperty
    {
        public string Name { get; }

        public string TypeName { get; }

        public string Initializer { get; }

        public GeneratedTypeProperty(string name, string typeName, string initializer)
        {
            Name = name;
            TypeName = typeName;
            Initializer = initializer;
        }
    }

    internal enum GeneratedTypeDefinitionKind
    {
        Object,
        List,
        PrimitiveWrapper,
    }
}
