using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sellorio.Generators.CSharpCommon.CodeGeneration;
using Sellorio.Generators.OpenApiCommon.Model;

namespace Sellorio.Generators.OpenApiClient.CodeGeneration
{
    internal static class OpenApiClientCodeGenerator
    {
        private static readonly StringComparer _tagComparer = StringComparer.Ordinal;

        public static OpenApiClientCodeGenerationResult Generate(OpenApiClientGenerationTarget generationTarget, OpenApiDocument document)
        {
            if (generationTarget == null)
            {
                throw new ArgumentNullException(nameof(generationTarget));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var operations = GetOperations(document, generationTarget).ToList();

            return new OpenApiClientCodeGenerationResult(
                GenerateInterfaceSource(generationTarget, operations),
                generationTarget.GenerateImplementation
                    ? GenerateImplementationSource(generationTarget, operations)
                    : null);
        }

        private static string GenerateInterfaceSource(OpenApiClientGenerationTarget generationTarget, IReadOnlyList<GeneratedOperation> operations)
        {
            var builder = new CSharpSourceBuilder();

            builder.AppendLine("using global::System.Collections.Generic;");
            builder.AppendLine("using global::System.Threading;");
            builder.AppendLine("using global::System.Threading.Tasks;");
            builder.AppendLine("using global::System.Text.Json;");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(generationTarget.NamespaceName))
            {
                builder.AppendLine("namespace " + generationTarget.NamespaceName + ";");
                builder.AppendLine();
            }

            using (builder.BeginBlock(generationTarget.InterfaceAccessibility + " partial interface " + generationTarget.InterfaceName))
            {
                foreach (var operation in operations)
                {
                    builder.AppendLine(operation.ReturnType + " " + operation.MethodName + "(" + string.Join(", ", GetDeclarationParameters(operation).Select(GenerateMethodParameterDeclaration)) + ");");
                }
            }

            return builder.ToString();
        }

        private static string GenerateImplementationSource(OpenApiClientGenerationTarget generationTarget, IReadOnlyList<GeneratedOperation> operations)
        {
            var builder = new CSharpSourceBuilder();

            builder.AppendLine("using global::System;");
            builder.AppendLine("using global::System.Collections.Generic;");
            builder.AppendLine("using global::System.Globalization;");
            builder.AppendLine("using global::System.Linq;");
            builder.AppendLine("using global::System.Net.Http;");
            builder.AppendLine("using global::System.Text;");
            builder.AppendLine("using global::System.Text.Json;");
            builder.AppendLine("using global::System.Threading;");
            builder.AppendLine("using global::System.Threading.Tasks;");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(generationTarget.NamespaceName))
            {
                builder.AppendLine("namespace " + generationTarget.NamespaceName + ";");
                builder.AppendLine();
            }

            using (builder.BeginBlock("internal partial class " + generationTarget.ClassName + " : " + generationTarget.InterfaceName))
            {
                builder.AppendLine("private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);");
                builder.AppendLine("private readonly HttpClient _httpClient;");
                builder.AppendLine();

                using (builder.BeginBlock("public " + generationTarget.ClassName + "(HttpClient httpClient)"))
                {
                    builder.AppendLine("_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
                }

                foreach (var operation in operations)
                {
                    builder.AppendLine();
                    GenerateImplementationMethod(builder, operation);
                }

                builder.AppendLine();
                using (builder.BeginBlock("private static void EnsureSuccessStatusCode(HttpResponseMessage response)"))
                {
                    builder.AppendLine("if (response.IsSuccessStatusCode)");
                    builder.AppendLine("{");
                    builder.AppendLine("    return;");
                    builder.AppendLine("}");
                    builder.AppendLine();
                    builder.AppendLine("throw new HttpRequestException($\"Request failed with status code {(int)response.StatusCode}.\", null, response.StatusCode);");
                }

                builder.AppendLine();
                using (builder.BeginBlock("private static async Task<JsonElement> ReadJsonElementAsync(HttpResponseMessage response)"))
                {
                    builder.AppendLine("var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);");
                    builder.AppendLine();
                    builder.AppendLine("if (string.IsNullOrWhiteSpace(content))");
                    builder.AppendLine("{");
                    builder.AppendLine("    return default;");
                    builder.AppendLine("}");
                    builder.AppendLine();
                    builder.AppendLine("using var document = JsonDocument.Parse(content);");
                    builder.AppendLine("return document.RootElement.Clone();");
                }

                builder.AppendLine();
                using (builder.BeginBlock("private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)"))
                {
                    builder.AppendLine("var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);");
                    builder.AppendLine("var value = JsonSerializer.Deserialize<T>(content, SerializerOptions);");
                    builder.AppendLine();
                    builder.AppendLine("if (value == null)");
                    builder.AppendLine("{");
                    builder.AppendLine("    throw new InvalidOperationException(\"Response body was null.\");");
                    builder.AppendLine("}");
                    builder.AppendLine();
                    builder.AppendLine("return value;");
                }
            }

            return builder.ToString();
        }

        private static void GenerateImplementationMethod(CSharpSourceBuilder builder, GeneratedOperation operation)
        {
            using (builder.BeginBlock("public async " + operation.ReturnType + " " + operation.MethodName + "(" + string.Join(", ", GetDeclarationParameters(operation).Select(GenerateMethodParameterDeclaration)) + ")"))
            {
                builder.AppendLine("var requestUriBuilder = new StringBuilder();");
                GeneratePathBuilder(builder, operation);
                builder.AppendLine("var queryParameters = new List<string>();");
                GenerateQueryParameters(builder, operation);
                builder.AppendLine("if (queryParameters.Count > 0)");
                builder.AppendLine("{");
                builder.AppendLine("    requestUriBuilder.Append('?');");
                builder.AppendLine("    requestUriBuilder.Append(string.Join(\"&\", queryParameters));");
                builder.AppendLine("}");
                builder.AppendLine();
                builder.AppendLine("using var request = new HttpRequestMessage(HttpMethod." + operation.HttpMethodName + ", requestUriBuilder.ToString());");
                GenerateHeaderAssignments(builder, operation);
                GenerateRequestBody(builder, operation);
                builder.AppendLine("using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);");
                builder.AppendLine("EnsureSuccessStatusCode(response);");

                if (operation.ResponseKind == ResponseKind.None)
                {
                    builder.AppendLine("return;");
                }
                else if (operation.ResponseKind == ResponseKind.JsonElement)
                {
                    builder.AppendLine("return await ReadJsonElementAsync(response).ConfigureAwait(false);");
                }
                else
                {
                    builder.AppendLine("return await ReadJsonAsync<" + operation.ResponseTypeName + ">(response).ConfigureAwait(false);");
                }
            }
        }

        private static void GeneratePathBuilder(CSharpSourceBuilder builder, GeneratedOperation operation)
        {
            var segments = operation.Path.Split(new[] { '{', '}' });
            var inParameter = false;

            foreach (var segment in segments)
            {
                if (!inParameter)
                {
                    if (segment.Length > 0)
                    {
                        builder.AppendLine("requestUriBuilder.Append(\"" + EscapeStringLiteral(segment) + "\");");
                    }
                }
                else
                {
                    var parameter = operation.Parameters.First(parameterInfo => parameterInfo.Kind == ParameterKind.Path && string.Equals(parameterInfo.OriginalName, segment, StringComparison.Ordinal));
                    builder.AppendLine("requestUriBuilder.Append(Uri.EscapeDataString(" + GetSerializationExpression(parameter, false) + ")); ");
                }

                inParameter = !inParameter;
            }
        }

        private static void GenerateQueryParameters(CSharpSourceBuilder builder, GeneratedOperation operation)
        {
            foreach (var parameter in operation.Parameters.Where(parameter => parameter.Kind == ParameterKind.Query))
            {
                if (parameter.IsOptional)
                {
                    builder.AppendLine("if (" + GetHasValueExpression(parameter) + ")");
                    builder.AppendLine("{");
                    builder.AppendLine("    queryParameters.Add(\"" + EscapeStringLiteral(parameter.OriginalName) + "=\" + Uri.EscapeDataString(" + GetSerializationExpression(parameter, true) + "));");
                    builder.AppendLine("}");
                }
                else
                {
                    builder.AppendLine("queryParameters.Add(\"" + EscapeStringLiteral(parameter.OriginalName) + "=\" + Uri.EscapeDataString(" + GetSerializationExpression(parameter, false) + "));");
                }
            }
        }

        private static void GenerateHeaderAssignments(CSharpSourceBuilder builder, GeneratedOperation operation)
        {
            foreach (var parameter in operation.Parameters.Where(parameter => parameter.Kind == ParameterKind.Header))
            {
                if (parameter.IsOptional)
                {
                    builder.AppendLine("if (" + GetHasValueExpression(parameter) + ")");
                    builder.AppendLine("{");
                    builder.AppendLine("    request.Headers.TryAddWithoutValidation(\"" + EscapeStringLiteral(parameter.OriginalName) + "\", " + GetSerializationExpression(parameter, true) + ");");
                    builder.AppendLine("}");
                }
                else
                {
                    builder.AppendLine("request.Headers.TryAddWithoutValidation(\"" + EscapeStringLiteral(parameter.OriginalName) + "\", " + GetSerializationExpression(parameter, false) + ");");
                }
            }
        }

        private static void GenerateRequestBody(CSharpSourceBuilder builder, GeneratedOperation operation)
        {
            if (operation.BodyParameter == null)
            {
                return;
            }

            if (operation.BodyParameter.IsOptional)
            {
                builder.AppendLine("if (" + GetHasValueExpression(operation.BodyParameter) + ")");
                builder.AppendLine("{");
                builder.AppendLine("    request.Content = new StringContent(JsonSerializer.Serialize(" + operation.BodyParameter.Name + ", SerializerOptions), Encoding.UTF8, \"" + EscapeStringLiteral(operation.BodyContentType) + "\");");
                builder.AppendLine("}");
            }
            else
            {
                builder.AppendLine("request.Content = new StringContent(JsonSerializer.Serialize(" + operation.BodyParameter.Name + ", SerializerOptions), Encoding.UTF8, \"" + EscapeStringLiteral(operation.BodyContentType) + "\");");
            }
        }

        private static IEnumerable<GeneratedOperation> GetOperations(OpenApiDocument document, OpenApiClientGenerationTarget generationTarget)
        {
            if (document.Paths == null)
            {
                yield break;
            }

            foreach (var pathItemPair in document.Paths.Items)
            {
                var path = pathItemPair.Key;
                var pathItem = pathItemPair.Value;

                foreach (var operation in GetOperations(pathItem))
                {
                    if (!ShouldIncludeOperation(generationTarget, operation.Operation))
                    {
                        continue;
                    }

                    yield return CreateOperation(document, path, pathItem, operation.HttpMethodName, operation.Operation);
                }
            }
        }

        private static IEnumerable<(string HttpMethodName, OpenApiOperation Operation)> GetOperations(OpenApiPathItem pathItem)
        {
            if (pathItem.Get != null)
            {
                yield return ("Get", pathItem.Get);
            }

            if (pathItem.Put != null)
            {
                yield return ("Put", pathItem.Put);
            }

            if (pathItem.Post != null)
            {
                yield return ("Post", pathItem.Post);
            }

            if (pathItem.Delete != null)
            {
                yield return ("Delete", pathItem.Delete);
            }

            if (pathItem.Options != null)
            {
                yield return ("Options", pathItem.Options);
            }

            if (pathItem.Head != null)
            {
                yield return ("Head", pathItem.Head);
            }

            if (pathItem.Patch != null)
            {
                yield return ("Patch", pathItem.Patch);
            }

            if (pathItem.Trace != null)
            {
                yield return ("Trace", pathItem.Trace);
            }
        }

        private static bool ShouldIncludeOperation(OpenApiClientGenerationTarget generationTarget, OpenApiOperation operation)
        {
            if (generationTarget.IncludedTags.Count == 0)
            {
                return true;
            }

            return operation.Tags.Any(tag => generationTarget.IncludedTags.Any(includedTag => _tagComparer.Equals(includedTag, tag)));
        }

        private static GeneratedOperation CreateOperation(OpenApiDocument document, string path, OpenApiPathItem pathItem, string httpMethodName, OpenApiOperation operation)
        {
            var parameters = new List<GeneratedParameter>();
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var parameterReference in pathItem.Parameters.Concat(operation.Parameters))
            {
                var parameter = ResolveParameter(document, parameterReference);
                if (parameter == null)
                {
                    continue;
                }

                parameters.Add(CreateParameter(parameter, usedNames));
            }

            GeneratedParameter bodyParameter = null;
            var requestBody = ResolveRequestBody(document, operation.RequestBody);
            var bodyContentType = GetPreferredContentType(requestBody?.Content);
            if (requestBody != null && bodyContentType != null)
            {
                var schema = requestBody.Content[bodyContentType].Schema;
                var parameterName = requestBody.Extensions.TryGetValue("x-parameter-name", out var extensionValue) && extensionValue is string configuredName && !string.IsNullOrWhiteSpace(configuredName)
                    ? configuredName
                    : "body";
                var bodyType = GetTypeName(schema, requestBody.Required == true, false, true);
                var bodyParameterName = GetUniqueName(CSharpIdentifier.ToCamelCase(parameterName), usedNames);
                bodyParameter = new GeneratedParameter(bodyParameterName, parameterName, bodyType, requestBody.Required != true, ParameterKind.Body, GetSchemaKind(schema));
                parameters.Add(bodyParameter);
            }

            parameters.Add(new GeneratedParameter("cancellationToken", "cancellationToken", "global::System.Threading.CancellationToken", true, ParameterKind.CancellationToken, SchemaKind.Other));

            var response = GetSuccessResponse(document, operation.Responses);
            var responseContentType = GetPreferredContentType(response?.Content);
            var responseSchema = response != null && responseContentType != null ? response.Content[responseContentType].Schema : null;
            var responseKind = GetResponseKind(responseSchema);
            var responseTypeName = responseKind == ResponseKind.None ? null : GetResponseTypeName(responseSchema);

            return new GeneratedOperation(
                CSharpIdentifier.ToPascalCase(string.IsNullOrWhiteSpace(operation.OperationId) ? httpMethodName + path : operation.OperationId),
                httpMethodName,
                path,
                parameters,
                bodyParameter,
                bodyContentType ?? "application/json",
                responseKind,
                responseTypeName == null ? "global::System.Threading.Tasks.Task" : "global::System.Threading.Tasks.Task<" + responseTypeName + ">",
                responseTypeName);
        }

        private static OpenApiParameter ResolveParameter(OpenApiDocument document, OpenApiReferenceOr<OpenApiParameter> parameterReference)
        {
            if (parameterReference == null)
            {
                return null;
            }

            if (parameterReference.Value != null)
            {
                return parameterReference.Value;
            }

            var reference = parameterReference.Reference?.Ref;
            if (reference == null || !reference.StartsWith("#/components/parameters/", StringComparison.Ordinal))
            {
                return null;
            }

            var name = reference.Substring("#/components/parameters/".Length);
            return document.Components != null && document.Components.Parameters.TryGetValue(name, out var referencedParameter)
                ? referencedParameter.Value
                : null;
        }

        private static OpenApiRequestBody ResolveRequestBody(OpenApiDocument document, OpenApiReferenceOr<OpenApiRequestBody> requestBodyReference)
        {
            if (requestBodyReference == null)
            {
                return null;
            }

            if (requestBodyReference.Value != null)
            {
                return requestBodyReference.Value;
            }

            var reference = requestBodyReference.Reference?.Ref;
            if (reference == null || !reference.StartsWith("#/components/requestBodies/", StringComparison.Ordinal))
            {
                return null;
            }

            var name = reference.Substring("#/components/requestBodies/".Length);
            return document.Components != null && document.Components.RequestBodies.TryGetValue(name, out var referencedRequestBody)
                ? referencedRequestBody.Value
                : null;
        }

        private static OpenApiResponse GetSuccessResponse(OpenApiDocument document, OpenApiResponses responses)
        {
            if (responses == null)
            {
                return null;
            }

            var successResponse = responses.Items
                .Where(pair => pair.Key.Length == 3 && pair.Key[0] == '2')
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => ResolveResponse(document, pair.Value))
                .FirstOrDefault(response => response != null);

            if (successResponse != null)
            {
                return successResponse;
            }

            return responses.TryGetValue("default", out var defaultResponse)
                ? ResolveResponse(document, defaultResponse)
                : null;
        }

        private static OpenApiResponse ResolveResponse(OpenApiDocument document, OpenApiReferenceOr<OpenApiResponse> responseReference)
        {
            if (responseReference == null)
            {
                return null;
            }

            if (responseReference.Value != null)
            {
                return responseReference.Value;
            }

            var reference = responseReference.Reference?.Ref;
            if (reference == null || !reference.StartsWith("#/components/responses/", StringComparison.Ordinal))
            {
                return null;
            }

            var name = reference.Substring("#/components/responses/".Length);
            return document.Components != null && document.Components.Responses.TryGetValue(name, out var referencedResponse)
                ? referencedResponse.Value
                : null;
        }

        private static string GetPreferredContentType(IDictionary<string, OpenApiMediaType> content)
        {
            if (content == null || content.Count == 0)
            {
                return null;
            }

            if (content.ContainsKey("application/json"))
            {
                return "application/json";
            }

            return content.Keys.First();
        }

        private static GeneratedParameter CreateParameter(OpenApiParameter parameter, HashSet<string> usedNames)
        {
            var name = GetUniqueName(CSharpIdentifier.ToCamelCase(parameter.Name), usedNames);
            return new GeneratedParameter(
                name,
                parameter.Name,
                GetTypeName(parameter.Schema, parameter.Required == true, false, false),
                parameter.Required != true,
                GetParameterKind(parameter.In),
                GetSchemaKind(parameter.Schema));
        }

        private static string GetTypeName(OpenApiSchema schema, bool required, bool forResponse, bool forBody)
        {
            if (schema == null)
            {
                return "string";
            }

            if (!string.IsNullOrWhiteSpace(schema.Ref))
            {
                return forResponse ? "global::System.Text.Json.JsonElement" : GetNullableType("global::System.Text.Json.JsonElement", required, false);
            }

            if (schema.Type is IList<object> || schema.OneOf.Count > 0 || schema.AnyOf.Count > 0 || schema.AllOf.Count > 0)
            {
                return "global::System.Text.Json.JsonElement";
            }

            var type = schema.Type as string;
            switch (type)
            {
                case "string":
                    if (string.Equals(schema.Format, "uuid", StringComparison.OrdinalIgnoreCase))
                    {
                        return GetNullableType("global::System.Guid", required, true);
                    }

                    if (string.Equals(schema.Format, "date-time", StringComparison.OrdinalIgnoreCase))
                    {
                        return GetNullableType("global::System.DateTimeOffset", required, true);
                    }

                    return "string";
                case "integer":
                    return GetNullableType("int", required, true);
                case "number":
                    return GetNullableType("decimal", required, true);
                case "boolean":
                    return GetNullableType("bool", required, true);
                case "array":
                    if (!forBody)
                    {
                        return "global::System.Text.Json.JsonElement";
                    }

                    var itemType = GetArrayItemTypeName(schema.Items);
                    return "global::System.Collections.Generic.IReadOnlyList<" + itemType + ">";
                case "object":
                default:
                    return "global::System.Text.Json.JsonElement";
            }
        }

        private static string GetArrayItemTypeName(OpenApiSchema schema)
        {
            if (schema == null || !string.IsNullOrWhiteSpace(schema.Ref))
            {
                return "global::System.Text.Json.JsonElement";
            }

            var type = schema.Type as string;
            switch (type)
            {
                case "string":
                    if (string.Equals(schema.Format, "uuid", StringComparison.OrdinalIgnoreCase))
                    {
                        return "global::System.Guid";
                    }

                    if (string.Equals(schema.Format, "date-time", StringComparison.OrdinalIgnoreCase))
                    {
                        return "global::System.DateTimeOffset";
                    }

                    return "string";
                case "integer":
                    return "int";
                case "number":
                    return "decimal";
                case "boolean":
                    return "bool";
                default:
                    return "global::System.Text.Json.JsonElement";
            }
        }

        private static ResponseKind GetResponseKind(OpenApiSchema schema)
        {
            if (schema == null)
            {
                return ResponseKind.None;
            }

            var typeName = GetResponseTypeName(schema);
            return typeName == "global::System.Text.Json.JsonElement"
                ? ResponseKind.JsonElement
                : ResponseKind.TypedJson;
        }

        private static string GetResponseTypeName(OpenApiSchema schema)
        {
            return GetTypeName(schema, true, true, false);
        }

        private static string GetNullableType(string typeName, bool required, bool isValueType)
        {
            if (required || !isValueType)
            {
                return typeName;
            }

            return typeName + "?";
        }

        private static ParameterKind GetParameterKind(string location)
        {
            switch (location)
            {
                case "path":
                    return ParameterKind.Path;
                case "query":
                    return ParameterKind.Query;
                case "header":
                    return ParameterKind.Header;
                default:
                    return ParameterKind.Query;
            }
        }

        private static SchemaKind GetSchemaKind(OpenApiSchema schema)
        {
            if (schema == null)
            {
                return SchemaKind.Other;
            }

            if (!string.IsNullOrWhiteSpace(schema.Ref))
            {
                return SchemaKind.JsonElement;
            }

            var type = schema.Type as string;
            switch (type)
            {
                case "string":
                    if (string.Equals(schema.Format, "uuid", StringComparison.OrdinalIgnoreCase))
                    {
                        return SchemaKind.Guid;
                    }

                    if (string.Equals(schema.Format, "date-time", StringComparison.OrdinalIgnoreCase))
                    {
                        return SchemaKind.DateTimeOffset;
                    }

                    return SchemaKind.String;
                case "integer":
                case "number":
                case "boolean":
                    return SchemaKind.Formattable;
                default:
                    return SchemaKind.JsonElement;
            }
        }

        private static string GenerateMethodParameterDeclaration(GeneratedParameter parameter)
        {
            if (parameter.Kind == ParameterKind.CancellationToken)
            {
                return parameter.TypeName + " " + parameter.Name + " = default";
            }

            if (!parameter.IsOptional)
            {
                return parameter.TypeName + " " + parameter.Name;
            }

            if (parameter.TypeName == "string")
            {
                return parameter.TypeName + " " + parameter.Name + " = null";
            }

            if (parameter.TypeName.StartsWith("global::System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal) || parameter.TypeName == "global::System.Text.Json.JsonElement")
            {
                return parameter.TypeName + " " + parameter.Name + " = default";
            }

            if (parameter.TypeName.EndsWith("?", StringComparison.Ordinal))
            {
                return parameter.TypeName + " " + parameter.Name + " = null";
            }

            return parameter.TypeName + " " + parameter.Name + " = default";
        }

        private static IEnumerable<GeneratedParameter> GetDeclarationParameters(GeneratedOperation operation)
        {
            return operation.Parameters
                .Where(parameter => parameter.Kind != ParameterKind.CancellationToken && !parameter.IsOptional)
                .Concat(operation.Parameters.Where(parameter => parameter.Kind != ParameterKind.CancellationToken && parameter.IsOptional))
                .Concat(operation.Parameters.Where(parameter => parameter.Kind == ParameterKind.CancellationToken));
        }

        private static string GetHasValueExpression(GeneratedParameter parameter)
        {
            if (parameter.TypeName == "string")
            {
                return parameter.Name + " != null";
            }

            if (parameter.TypeName.StartsWith("global::System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal))
            {
                return parameter.Name + " != null";
            }

            if (parameter.TypeName == "global::System.Text.Json.JsonElement")
            {
                return parameter.Name + ".ValueKind != JsonValueKind.Undefined";
            }

            if (parameter.TypeName.EndsWith("?", StringComparison.Ordinal))
            {
                return parameter.Name + ".HasValue";
            }

            return parameter.Name + " != null";
        }

        private static string GetSerializationExpression(GeneratedParameter parameter, bool unwrapOptional)
        {
            var valueExpression = unwrapOptional && parameter.TypeName.EndsWith("?", StringComparison.Ordinal)
                ? parameter.Name + ".Value"
                : parameter.Name;

            switch (parameter.SchemaKind)
            {
                case SchemaKind.Guid:
                    return valueExpression + ".ToString(\"D\", CultureInfo.InvariantCulture)";
                case SchemaKind.DateTimeOffset:
                    return valueExpression + ".ToString(\"O\", CultureInfo.InvariantCulture)";
                case SchemaKind.String:
                    return valueExpression;
                case SchemaKind.Formattable:
                    return valueExpression + ".ToString(CultureInfo.InvariantCulture)";
                case SchemaKind.JsonElement:
                case SchemaKind.Other:
                default:
                    return "JsonSerializer.Serialize(" + valueExpression + ", SerializerOptions)";
            }
        }

        private static string GetUniqueName(string candidate, ISet<string> usedNames)
        {
            var baseName = string.IsNullOrWhiteSpace(candidate) ? "value" : candidate;
            var uniqueName = baseName;
            var suffix = 2;

            while (!usedNames.Add(uniqueName))
            {
                uniqueName = baseName + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }

            return uniqueName;
        }

        private static string EscapeStringLiteral(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    internal sealed class OpenApiClientCodeGenerationResult
    {
        public string InterfaceSource { get; }

        public string ImplementationSource { get; }

        public OpenApiClientCodeGenerationResult(string interfaceSource, string implementationSource)
        {
            InterfaceSource = interfaceSource;
            ImplementationSource = implementationSource;
        }
    }

    internal sealed class OpenApiClientGenerationTarget
    {
        public string NamespaceName { get; }

        public string InterfaceAccessibility { get; }

        public string InterfaceName { get; }

        public string ClassName { get; }

        public string OpenApiDefinitionFilename { get; }

        public bool GenerateImplementation { get; }

        public IReadOnlyList<string> IncludedTags { get; }

        public OpenApiClientGenerationTarget(
            string namespaceName,
            string interfaceAccessibility,
            string interfaceName,
            string className,
            string openApiDefinitionFilename,
            bool generateImplementation,
            IReadOnlyList<string> includedTags)
        {
            NamespaceName = namespaceName;
            InterfaceAccessibility = interfaceAccessibility;
            InterfaceName = interfaceName;
            ClassName = className;
            OpenApiDefinitionFilename = openApiDefinitionFilename;
            GenerateImplementation = generateImplementation;
            IncludedTags = includedTags ?? Array.Empty<string>();
        }
    }

    internal sealed class GeneratedOperation
    {
        public string MethodName { get; }

        public string HttpMethodName { get; }

        public string Path { get; }

        public IReadOnlyList<GeneratedParameter> Parameters { get; }

        public GeneratedParameter BodyParameter { get; }

        public string BodyContentType { get; }

        public ResponseKind ResponseKind { get; }

        public string ReturnType { get; }

        public string ResponseTypeName { get; }

        public GeneratedOperation(
            string methodName,
            string httpMethodName,
            string path,
            IReadOnlyList<GeneratedParameter> parameters,
            GeneratedParameter bodyParameter,
            string bodyContentType,
            ResponseKind responseKind,
            string returnType,
            string responseTypeName)
        {
            MethodName = methodName;
            HttpMethodName = httpMethodName;
            Path = path;
            Parameters = parameters;
            BodyParameter = bodyParameter;
            BodyContentType = bodyContentType;
            ResponseKind = responseKind;
            ReturnType = returnType;
            ResponseTypeName = responseTypeName;
        }
    }

    internal sealed class GeneratedParameter
    {
        public string Name { get; }

        public string OriginalName { get; }

        public string TypeName { get; }

        public bool IsOptional { get; }

        public ParameterKind Kind { get; }

        public SchemaKind SchemaKind { get; }

        public GeneratedParameter(
            string name,
            string originalName,
            string typeName,
            bool isOptional,
            ParameterKind kind,
            SchemaKind schemaKind)
        {
            Name = name;
            OriginalName = originalName;
            TypeName = typeName;
            IsOptional = isOptional;
            Kind = kind;
            SchemaKind = schemaKind;
        }
    }

    internal enum ParameterKind
    {
        Path,
        Query,
        Header,
        Body,
        CancellationToken,
    }

    internal enum ResponseKind
    {
        None,
        JsonElement,
        TypedJson,
    }

    internal enum SchemaKind
    {
        String,
        Guid,
        DateTimeOffset,
        Formattable,
        JsonElement,
        Other,
    }
}
