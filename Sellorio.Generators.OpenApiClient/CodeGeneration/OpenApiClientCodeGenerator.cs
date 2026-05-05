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

            var schemaTypeRegistry = new OpenApiClientSchemaTypeRegistry(document, generationTarget.InterfaceAccessibility);
            var operations = GetOperations(document, generationTarget, schemaTypeRegistry).ToList();
            var generatedTypesSource = schemaTypeRegistry.GenerateTypeDefinitions();

            return new OpenApiClientCodeGenerationResult(
                GenerateInterfaceSource(generationTarget, operations, generatedTypesSource),
                generationTarget.GenerateImplementation
                    ? GenerateImplementationSource(generationTarget, operations)
                    : null);
        }

        private static string GenerateInterfaceSource(OpenApiClientGenerationTarget generationTarget, IReadOnlyList<GeneratedOperation> operations, string generatedTypesSource)
        {
            var builder = new CSharpSourceBuilder();

            builder.AppendLine("#nullable enable");
            builder.AppendLine("using global::System.Threading;");
            builder.AppendLine("using global::System.Threading.Tasks;");
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

            if (!string.IsNullOrWhiteSpace(generatedTypesSource))
            {
                builder.AppendLine();
                builder.AppendLine(generatedTypesSource.TrimEnd());
            }

            return builder.ToString();
        }

        private static string GenerateImplementationSource(OpenApiClientGenerationTarget generationTarget, IReadOnlyList<GeneratedOperation> operations)
        {
            var builder = new CSharpSourceBuilder();

            builder.AppendLine("#nullable enable");
            builder.AppendLine("using global::System;");
            builder.AppendLine("using global::System.Collections.Generic;");
            builder.AppendLine("using global::System.Globalization;");
            builder.AppendLine("using global::System.Linq;");
            builder.AppendLine("using global::System.Net;");
            builder.AppendLine("using global::System.Net.Http;");
            builder.AppendLine("using global::System.Text;");
            builder.AppendLine("using global::System.Text.Json;");
            builder.AppendLine("using global::System.Threading;");
            builder.AppendLine("using global::System.Threading.Tasks;");
            builder.AppendLine("using global::Sellorio.Clients.Rest;");
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
                using (builder.BeginBlock("private static async Task<T> DeserializeSuccessResponseAsync<T>(HttpResponseMessage response)"))
                {
                    builder.AppendLine("var result = await Task.FromResult(response).ToValueResult<T>(SerializerOptions).ConfigureAwait(false);");
                    builder.AppendLine("return result.WasSuccess");
                    builder.AppendLine("    ? result.Value");
                    builder.AppendLine("    : throw new InvalidOperationException(\"Expected a successful response body.\");");
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
                builder.AppendLine("var responseTask = _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);");

                if (operation.ReturnStrategy.Kind == ResultReturnKind.Result)
                {
                    if (operation.ReturnStrategy.ContextTypeName == null)
                    {
                        builder.AppendLine("return await responseTask.ToResult(SerializerOptions).ConfigureAwait(false);");
                    }
                    else
                    {
                        builder.AppendLine("return await responseTask.ToResult<" + operation.ReturnStrategy.ContextTypeName + ">(SerializerOptions).ConfigureAwait(false);");
                    }
                }
                else if (operation.ReturnStrategy.SuccessResponses.Count == 1)
                {
                    if (operation.ReturnStrategy.ContextTypeName == null)
                    {
                        builder.AppendLine("return await responseTask.ToValueResult<" + operation.ReturnStrategy.ValueTypeName + ">(SerializerOptions).ConfigureAwait(false);");
                    }
                    else
                    {
                        builder.AppendLine("return await responseTask.ToValueResult<" + operation.ReturnStrategy.ContextTypeName + ", " + operation.ReturnStrategy.ValueTypeName + ">(SerializerOptions).ConfigureAwait(false);");
                    }
                }
                else
                {
                    var methodName = operation.ReturnStrategy.ContextTypeName == null
                        ? "ToValueResult<" + operation.ReturnStrategy.ValueTypeName + ">"
                        : "ToValueResult<" + operation.ReturnStrategy.ContextTypeName + ", " + operation.ReturnStrategy.ValueTypeName + ">";
                    builder.AppendLine("return await responseTask." + methodName + "(");
                    builder.AppendLine("    async response =>");
                    builder.AppendLine("    {");
                    builder.AppendLine("        switch (response.StatusCode)");
                    builder.AppendLine("        {");

                    foreach (var successResponse in operation.ReturnStrategy.SuccessResponses)
                    {
                        var caseLabel = GetHttpStatusCodeCaseLabel(successResponse.StatusCode);
                        if (caseLabel == "default")
                        {
                            builder.AppendLine("            default:");
                        }
                        else
                        {
                            builder.AppendLine("            case " + caseLabel + ":");
                        }

                        if (successResponse.RequiresWrapping)
                        {
                            builder.AppendLine("            {");
                            builder.AppendLine("                var value = await DeserializeSuccessResponseAsync<" + successResponse.DeserializeTypeName + ">(response).ConfigureAwait(false);");
                            builder.AppendLine("                return new " + successResponse.ConcreteTypeName + " { Value = value };");
                            builder.AppendLine("            }");
                        }
                        else
                        {
                            builder.AppendLine("            {");
                            builder.AppendLine("                return await DeserializeSuccessResponseAsync<" + successResponse.DeserializeTypeName + ">(response).ConfigureAwait(false);");
                            builder.AppendLine("            }");
                        }
                    }

                    builder.AppendLine("            default:");
                    builder.AppendLine("                throw new InvalidOperationException($\"Unexpected success status code {(int)response.StatusCode}.\");");
                    builder.AppendLine("        }");
                    builder.AppendLine("    }, SerializerOptions).ConfigureAwait(false);");
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

        private static IEnumerable<GeneratedOperation> GetOperations(OpenApiDocument document, OpenApiClientGenerationTarget generationTarget, OpenApiClientSchemaTypeRegistry schemaTypeRegistry)
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

                    yield return CreateOperation(document, path, pathItem, operation.HttpMethodName, operation.Operation, schemaTypeRegistry);
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

        private static GeneratedOperation CreateOperation(OpenApiDocument document, string path, OpenApiPathItem pathItem, string httpMethodName, OpenApiOperation operation, OpenApiClientSchemaTypeRegistry schemaTypeRegistry)
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

                parameters.Add(CreateParameter(parameter, usedNames, schemaTypeRegistry));
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
                var resolvedBodyType = schemaTypeRegistry.ResolveSchemaType(schema, CSharpIdentifier.ToPascalCase(parameterName), requestBody.Required == true);
                var bodyType = resolvedBodyType.TypeName;
                var bodyParameterName = GetUniqueName(CSharpIdentifier.ToCamelCase(parameterName), usedNames);
                bodyParameter = new GeneratedParameter(bodyParameterName, parameterName, bodyType, requestBody.Required != true, ParameterKind.Body, GetSchemaKind(schema), resolvedBodyType.IsSpecificModel, resolvedBodyType.NonNullableTypeName);
                parameters.Add(bodyParameter);
            }

            parameters.Add(new GeneratedParameter("cancellationToken", "cancellationToken", "global::System.Threading.CancellationToken", true, ParameterKind.CancellationToken, SchemaKind.Other, false, "global::System.Threading.CancellationToken"));

            var returnStrategy = CreateReturnStrategy(document, operation, schemaTypeRegistry, bodyParameter);

            return new GeneratedOperation(
                CSharpIdentifier.ToPascalCase(string.IsNullOrWhiteSpace(operation.OperationId) ? httpMethodName + path : operation.OperationId),
                httpMethodName,
                path,
                parameters,
                bodyParameter,
                bodyContentType ?? "application/json",
                returnStrategy);
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

        private static GeneratedReturnStrategy CreateReturnStrategy(OpenApiDocument document, OpenApiOperation operation, OpenApiClientSchemaTypeRegistry schemaTypeRegistry, GeneratedParameter bodyParameter)
        {
            var requestModelContextTypeName = bodyParameter?.NonNullableTypeName;
            var successResponses = GetSuccessResponses(document, operation.Responses).ToList();
            var responseModels = successResponses
                .Select(response =>
                {
                    var contentType = GetPreferredContentType(response.Response?.Content);
                    var schema = contentType == null ? null : response.Response.Content[contentType].Schema;
                    return new { response.StatusCode, Schema = schema };
                })
                .Where(response => response.Schema != null)
                .ToList();

            if (responseModels.Count == 0)
            {
                return requestModelContextTypeName == null
                    ? GeneratedReturnStrategy.CreateResult("global::System.Threading.Tasks.Task<global::Sellorio.Results.Result>")
                    : GeneratedReturnStrategy.CreateResult(
                        "global::System.Threading.Tasks.Task<global::Sellorio.Results.Result<" + requestModelContextTypeName + ">>",
                        requestModelContextTypeName);
            }

            if (responseModels.Count == 1)
            {
                var responseModel = responseModels[0];
                var preferredName = CSharpIdentifier.ToPascalCase(string.IsNullOrWhiteSpace(operation.OperationId) ? "Response" : operation.OperationId + "Response");
                var resolvedType = schemaTypeRegistry.ResolveSchemaType(responseModel.Schema, preferredName, true);
                var successResponse = new GeneratedSuccessResponse(responseModel.StatusCode, resolvedType.NonNullableTypeName, resolvedType.NonNullableTypeName, false);

                if (requestModelContextTypeName != null)
                {
                    return GeneratedReturnStrategy.CreateValueResult(
                        "global::System.Threading.Tasks.Task<global::Sellorio.Results.ValueResult<" + requestModelContextTypeName + ", " + resolvedType.NonNullableTypeName + ">>",
                        requestModelContextTypeName,
                        resolvedType.NonNullableTypeName,
                        successResponse);
                }

                return GeneratedReturnStrategy.CreateValueResult(
                    "global::System.Threading.Tasks.Task<global::Sellorio.Results.ValueResult<" + resolvedType.NonNullableTypeName + ">>",
                    null,
                    resolvedType.NonNullableTypeName,
                    successResponse);
            }

            var interfaceName = "I" + CSharpIdentifier.ToPascalCase(string.IsNullOrWhiteSpace(operation.OperationId) ? "Response" : operation.OperationId + "Response");
            var generatedResponses = new List<GeneratedSuccessResponse>();

            foreach (var responseModel in responseModels)
            {
                var preferredName = CSharpIdentifier.ToPascalCase(string.IsNullOrWhiteSpace(operation.OperationId)
                    ? "Response" + responseModel.StatusCode
                    : operation.OperationId + responseModel.StatusCode + "Response");
                var successType = schemaTypeRegistry.ResolvePolymorphicSuccessResponseType(responseModel.Schema, preferredName, interfaceName);
                generatedResponses.Add(new GeneratedSuccessResponse(responseModel.StatusCode, successType.ConcreteTypeName, successType.DeserializeTypeName, successType.RequiresWrapping));
            }

            return requestModelContextTypeName == null
                ? GeneratedReturnStrategy.CreateValueResult(
                    "global::System.Threading.Tasks.Task<global::Sellorio.Results.ValueResult<" + interfaceName + ">>",
                    null,
                    interfaceName,
                    generatedResponses.ToArray())
                : GeneratedReturnStrategy.CreateValueResult(
                    "global::System.Threading.Tasks.Task<global::Sellorio.Results.ValueResult<" + requestModelContextTypeName + ", " + interfaceName + ">>",
                    requestModelContextTypeName,
                    interfaceName,
                    generatedResponses.ToArray());
        }

        private static IEnumerable<(string StatusCode, OpenApiResponse Response)> GetSuccessResponses(OpenApiDocument document, OpenApiResponses responses)
        {
            if (responses == null)
            {
                yield break;
            }

            var successResponses = responses.Items
                .Where(pair => pair.Key.Length == 3 && pair.Key[0] == '2')
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => (pair.Key, ResolveResponse(document, pair.Value)))
                .Where(pair => pair.Item2 != null)
                .ToList();

            if (successResponses.Count > 0)
            {
                foreach (var successResponse in successResponses)
                {
                    yield return successResponse;
                }

                yield break;
            }

            if (responses.TryGetValue("default", out var defaultResponse))
            {
                var resolvedResponse = ResolveResponse(document, defaultResponse);
                if (resolvedResponse != null)
                {
                    yield return ("default", resolvedResponse);
                }
            }
        }

        private static string GetHttpStatusCodeCaseLabel(string statusCode)
        {
            return string.Equals(statusCode, "default", StringComparison.Ordinal)
                ? "default"
                : "(HttpStatusCode)" + statusCode;
        }

        private static GeneratedParameter CreateParameter(OpenApiParameter parameter, HashSet<string> usedNames, OpenApiClientSchemaTypeRegistry schemaTypeRegistry)
        {
            var name = GetUniqueName(CSharpIdentifier.ToCamelCase(parameter.Name), usedNames);
            var resolvedType = schemaTypeRegistry.ResolveSchemaType(parameter.Schema, CSharpIdentifier.ToPascalCase(parameter.Name), parameter.Required == true);
            return new GeneratedParameter(
                name,
                parameter.Name,
                resolvedType.TypeName,
                parameter.Required != true,
                GetParameterKind(parameter.In),
                GetSchemaKind(parameter.Schema),
                resolvedType.IsSpecificModel,
                resolvedType.NonNullableTypeName);
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

            if (IsNullableReferenceType(parameter) || parameter.TypeName == "string")
            {
                return parameter.TypeName + " " + parameter.Name + " = null";
            }

            if (parameter.TypeName.StartsWith("global::System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal) || parameter.TypeName == "global::System.Text.Json.JsonElement")
            {
                return parameter.TypeName + " " + parameter.Name + " = default";
            }

            if (IsNullableValueType(parameter))
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
            if (parameter.TypeName == "string" || IsNullableReferenceType(parameter))
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

            if (IsNullableValueType(parameter))
            {
                return parameter.Name + ".HasValue";
            }

            return parameter.Name + " != null";
        }

        private static string GetSerializationExpression(GeneratedParameter parameter, bool unwrapOptional)
        {
            var valueExpression = unwrapOptional && IsNullableValueType(parameter)
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

        private static bool IsNullableValueType(GeneratedParameter parameter)
        {
            return parameter.TypeName.EndsWith("?", StringComparison.Ordinal)
                && parameter.SchemaKind != SchemaKind.String
                && !parameter.TypeName.StartsWith("global::System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal);
        }

        private static bool IsNullableReferenceType(GeneratedParameter parameter)
        {
            return parameter.TypeName.EndsWith("?", StringComparison.Ordinal)
                && !IsNullableValueType(parameter);
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

        public string ReturnType { get; }

        public GeneratedReturnStrategy ReturnStrategy { get; }

        public GeneratedOperation(
            string methodName,
            string httpMethodName,
            string path,
            IReadOnlyList<GeneratedParameter> parameters,
            GeneratedParameter bodyParameter,
            string bodyContentType,
            GeneratedReturnStrategy returnStrategy)
        {
            MethodName = methodName;
            HttpMethodName = httpMethodName;
            Path = path;
            Parameters = parameters;
            BodyParameter = bodyParameter;
            BodyContentType = bodyContentType;
            ReturnStrategy = returnStrategy;
            ReturnType = returnStrategy.ReturnType;
        }
    }

    internal sealed class GeneratedReturnStrategy
    {
        public ResultReturnKind Kind { get; }

        public string ReturnType { get; }

        public string ContextTypeName { get; }

        public string ValueTypeName { get; }

        public IReadOnlyList<GeneratedSuccessResponse> SuccessResponses { get; }

        private GeneratedReturnStrategy(ResultReturnKind kind, string returnType, string contextTypeName, string valueTypeName, IReadOnlyList<GeneratedSuccessResponse> successResponses)
        {
            Kind = kind;
            ReturnType = returnType;
            ContextTypeName = contextTypeName;
            ValueTypeName = valueTypeName;
            SuccessResponses = successResponses;
        }

        public static GeneratedReturnStrategy CreateResult(string returnType, string contextTypeName = null)
        {
            return new GeneratedReturnStrategy(ResultReturnKind.Result, returnType, contextTypeName, null, Array.Empty<GeneratedSuccessResponse>());
        }

        public static GeneratedReturnStrategy CreateValueResult(string returnType, string contextTypeName, string valueTypeName, params GeneratedSuccessResponse[] successResponses)
        {
            return new GeneratedReturnStrategy(ResultReturnKind.ValueResult, returnType, contextTypeName, valueTypeName, successResponses ?? Array.Empty<GeneratedSuccessResponse>());
        }
    }

    internal sealed class GeneratedSuccessResponse
    {
        public string StatusCode { get; }

        public string ConcreteTypeName { get; }

        public string DeserializeTypeName { get; }

        public bool RequiresWrapping { get; }

        public GeneratedSuccessResponse(string statusCode, string concreteTypeName, string deserializeTypeName, bool requiresWrapping)
        {
            StatusCode = statusCode;
            ConcreteTypeName = concreteTypeName;
            DeserializeTypeName = deserializeTypeName;
            RequiresWrapping = requiresWrapping;
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

        public bool IsSpecificModel { get; }

        public string NonNullableTypeName { get; }

        public GeneratedParameter(
            string name,
            string originalName,
            string typeName,
            bool isOptional,
            ParameterKind kind,
            SchemaKind schemaKind,
            bool isSpecificModel,
            string nonNullableTypeName)
        {
            Name = name;
            OriginalName = originalName;
            TypeName = typeName;
            IsOptional = isOptional;
            Kind = kind;
            SchemaKind = schemaKind;
            IsSpecificModel = isSpecificModel;
            NonNullableTypeName = nonNullableTypeName;
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

    internal enum ResultReturnKind
    {
        Result,
        ValueResult,
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
