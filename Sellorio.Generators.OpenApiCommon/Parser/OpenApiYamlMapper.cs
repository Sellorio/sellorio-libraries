using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Sellorio.Generators.OpenApiCommon.Model;

namespace Sellorio.Generators.OpenApiCommon.Parser
{
    internal static class OpenApiYamlMapper
    {
        public static OpenApiDocument Parse(string yaml)
        {
            var root = DeserializeRoot(yaml);
            var document = new OpenApiDocument();

            document.OpenApi = GetString(root, "openapi");
            document.JsonSchemaDialect = GetString(root, "jsonSchemaDialect");
            document.Info = MapObject(root, "info", MapInfo);
            document.Servers = MapList(root, "servers", MapServer);
            document.Paths = MapObject(root, "paths", MapPaths);
            document.Webhooks = MapDictionary(root, "webhooks", MapReferenceOrPathItem);
            document.Components = MapObject(root, "components", MapComponents);
            document.Security = MapList(root, "security", MapSecurityRequirement);
            document.Tags = MapList(root, "tags", MapTag);
            document.ExternalDocs = MapObject(root, "externalDocs", MapExternalDocumentation);
            PopulateExtensions(document.Extensions, root, "openapi", "jsonSchemaDialect", "info", "servers", "paths", "webhooks", "components", "security", "tags", "externalDocs");

            return document;
        }

        private static IDictionary<object, object> DeserializeRoot(string yaml)
        {
            return SimpleYamlParser.Parse(yaml);
        }

        private static OpenApiInfo MapInfo(object value)
        {
            if (TryConvertToScalarString(value, out var title))
            {
                return new OpenApiInfo
                {
                    Title = title,
                };
            }

            return MapObject(value, mapping =>
            {
                var info = new OpenApiInfo();
                info.Title = GetString(mapping, "title");
                info.Summary = GetString(mapping, "summary");
                info.Description = GetString(mapping, "description");
                info.TermsOfService = GetString(mapping, "termsOfService");
                info.Contact = MapObject(mapping, "contact", MapContact);
                info.License = MapObject(mapping, "license", MapLicense);
                info.Version = GetString(mapping, "version");
                PopulateExtensions(info.Extensions, mapping, "title", "summary", "description", "termsOfService", "contact", "license", "version");
                return info;
            });
        }

        private static OpenApiContact MapContact(object value)
        {
            return MapObject(value, mapping =>
            {
                var contact = new OpenApiContact();
                contact.Name = GetString(mapping, "name");
                contact.Url = GetString(mapping, "url");
                contact.Email = GetString(mapping, "email");
                PopulateExtensions(contact.Extensions, mapping, "name", "url", "email");
                return contact;
            });
        }

        private static OpenApiLicense MapLicense(object value)
        {
            return MapObject(value, mapping =>
            {
                var license = new OpenApiLicense();
                license.Name = GetString(mapping, "name");
                license.Identifier = GetString(mapping, "identifier");
                license.Url = GetString(mapping, "url");
                PopulateExtensions(license.Extensions, mapping, "name", "identifier", "url");
                return license;
            });
        }

        private static OpenApiServer MapServer(object value)
        {
            if (TryConvertToScalarString(value, out var url))
            {
                return new OpenApiServer
                {
                    Url = url,
                };
            }

            return MapObject(value, mapping =>
            {
                var server = new OpenApiServer();
                server.Url = GetString(mapping, "url");
                server.Description = GetString(mapping, "description");
                server.Variables = MapDictionary(mapping, "variables", MapServerVariable);
                PopulateExtensions(server.Extensions, mapping, "url", "description", "variables");
                return server;
            });
        }

        private static OpenApiServerVariable MapServerVariable(object value)
        {
            return MapObject(value, mapping =>
            {
                var variable = new OpenApiServerVariable();
                variable.Enum = MapScalarList(mapping, "enum");
                variable.Default = GetString(mapping, "default");
                variable.Description = GetString(mapping, "description");
                PopulateExtensions(variable.Extensions, mapping, "enum", "default", "description");
                return variable;
            });
        }

        private static OpenApiPaths MapPaths(object value)
        {
            return MapObject(value, mapping =>
            {
                var paths = new OpenApiPaths();
                foreach (var pair in mapping)
                {
                    var key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
                    if (key != null && key.StartsWith("x-", StringComparison.Ordinal))
                    {
                        paths.Extensions[key] = NormalizeValue(pair.Value);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(key) && !key.StartsWith("/", StringComparison.Ordinal))
                    {
                        key = "/" + key;
                    }

                    paths[key] = MapPathItem(pair.Value);
                }

                return paths;
            });
        }

        private static OpenApiPathItem MapPathItem(object value)
        {
            return MapObject(value, mapping =>
            {
                var pathItem = new OpenApiPathItem();
                pathItem.Ref = GetString(mapping, "$ref");
                pathItem.Summary = GetString(mapping, "summary");
                pathItem.Description = GetString(mapping, "description");
                pathItem.Get = MapObject(mapping, "get", MapOperation);
                pathItem.Put = MapObject(mapping, "put", MapOperation);
                pathItem.Post = MapObject(mapping, "post", MapOperation);
                pathItem.Delete = MapObject(mapping, "delete", MapOperation);
                pathItem.Options = MapObject(mapping, "options", MapOperation);
                pathItem.Head = MapObject(mapping, "head", MapOperation);
                pathItem.Patch = MapObject(mapping, "patch", MapOperation);
                pathItem.Trace = MapObject(mapping, "trace", MapOperation);
                pathItem.Servers = MapList(mapping, "servers", MapServer);
                pathItem.Parameters = MapList(mapping, "parameters", MapReferenceOrParameter);
                PopulateExtensions(pathItem.Extensions, mapping, "$ref", "summary", "description", "get", "put", "post", "delete", "options", "head", "patch", "trace", "servers", "parameters");
                return pathItem;
            });
        }

        private static OpenApiOperation MapOperation(object value)
        {
            return MapObject(value, mapping =>
            {
                var operation = new OpenApiOperation();
                operation.Tags = MapScalarList(mapping, "tags");
                operation.Summary = GetString(mapping, "summary");
                operation.Description = GetString(mapping, "description");
                operation.ExternalDocs = MapObject(mapping, "externalDocs", MapExternalDocumentation);
                operation.OperationId = GetString(mapping, "operationId");
                operation.Parameters = MapList(mapping, "parameters", MapReferenceOrParameter);
                operation.RequestBody = MapObject(mapping, "requestBody", MapReferenceOrRequestBody);
                operation.Responses = MapObject(mapping, "responses", MapResponses);
                operation.Callbacks = MapDictionary(mapping, "callbacks", MapReferenceOrCallback);
                operation.Deprecated = GetNullableBoolean(mapping, "deprecated");
                operation.Security = MapList(mapping, "security", MapSecurityRequirement);
                operation.Servers = MapList(mapping, "servers", MapServer);
                PopulateExtensions(operation.Extensions, mapping, "tags", "summary", "description", "externalDocs", "operationId", "parameters", "requestBody", "responses", "callbacks", "deprecated", "security", "servers");
                return operation;
            });
        }

        private static OpenApiParameter MapParameter(object value)
        {
            return MapObject(value, mapping =>
            {
                var parameter = new OpenApiParameter();
                MapParameterLike(mapping, parameter);
                parameter.Name = GetString(mapping, "name");
                parameter.In = GetString(mapping, "in");
                PopulateExtensions(parameter.Extensions, mapping, "description", "required", "deprecated", "allowEmptyValue", "style", "explode", "allowReserved", "schema", "example", "examples", "content", "name", "in");
                return parameter;
            });
        }

        private static OpenApiHeader MapHeader(object value)
        {
            return MapObject(value, mapping =>
            {
                var header = new OpenApiHeader();
                MapParameterLike(mapping, header);
                PopulateExtensions(header.Extensions, mapping, "description", "required", "deprecated", "allowEmptyValue", "style", "explode", "allowReserved", "schema", "example", "examples", "content");
                return header;
            });
        }

        private static void MapParameterLike(IDictionary<object, object> mapping, OpenApiParameterLikeObject parameterLike)
        {
            parameterLike.Description = GetString(mapping, "description");
            parameterLike.Required = GetNullableBoolean(mapping, "required");
            parameterLike.Deprecated = GetNullableBoolean(mapping, "deprecated");
            parameterLike.AllowEmptyValue = GetNullableBoolean(mapping, "allowEmptyValue");
            parameterLike.Style = GetString(mapping, "style");
            parameterLike.Explode = GetNullableBoolean(mapping, "explode");
            parameterLike.AllowReserved = GetNullableBoolean(mapping, "allowReserved");
            parameterLike.Schema = MapObject(mapping, "schema", MapSchema);
            parameterLike.Example = GetValue(mapping, "example");
            parameterLike.Examples = MapDictionary(mapping, "examples", MapReferenceOrExample);
            parameterLike.Content = MapDictionary(mapping, "content", MapMediaType);
        }

        private static OpenApiRequestBody MapRequestBody(object value)
        {
            return MapObject(value, mapping =>
            {
                var requestBody = new OpenApiRequestBody();
                requestBody.Description = GetString(mapping, "description");
                requestBody.Content = MapDictionary(mapping, "content", MapMediaType);
                requestBody.Required = GetNullableBoolean(mapping, "required");
                PopulateExtensions(requestBody.Extensions, mapping, "description", "content", "required");
                return requestBody;
            });
        }

        private static OpenApiMediaType MapMediaType(object value)
        {
            if (!TryConvertToMapping(value, out _))
            {
                return new OpenApiMediaType
                {
                    Schema = MapSchema(value),
                };
            }

            return MapObject(value, mapping =>
            {
                var mediaType = new OpenApiMediaType();
                mediaType.Schema = MapObject(mapping, "schema", MapSchema);
                mediaType.Example = GetValue(mapping, "example");
                mediaType.Examples = MapDictionary(mapping, "examples", MapReferenceOrExample);
                mediaType.Encoding = MapDictionary(mapping, "encoding", MapEncoding);
                PopulateExtensions(mediaType.Extensions, mapping, "schema", "example", "examples", "encoding");
                return mediaType;
            });
        }

        private static OpenApiEncoding MapEncoding(object value)
        {
            return MapObject(value, mapping =>
            {
                var encoding = new OpenApiEncoding();
                encoding.ContentType = GetString(mapping, "contentType");
                encoding.Headers = MapDictionary(mapping, "headers", MapReferenceOrHeader);
                encoding.Style = GetString(mapping, "style");
                encoding.Explode = GetNullableBoolean(mapping, "explode");
                encoding.AllowReserved = GetNullableBoolean(mapping, "allowReserved");
                PopulateExtensions(encoding.Extensions, mapping, "contentType", "headers", "style", "explode", "allowReserved");
                return encoding;
            });
        }

        private static OpenApiResponses MapResponses(object value)
        {
            return MapObject(value, mapping =>
            {
                var responses = new OpenApiResponses();
                foreach (var pair in mapping)
                {
                    var key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
                    if (key != null && key.StartsWith("x-", StringComparison.Ordinal))
                    {
                        responses.Extensions[key] = NormalizeValue(pair.Value);
                        continue;
                    }

                    responses[key] = MapReferenceOrResponse(pair.Value);
                }

                return responses;
            });
        }

        private static OpenApiResponse MapResponse(object value)
        {
            if (TryConvertToScalarString(value, out var description))
            {
                return new OpenApiResponse
                {
                    Description = description,
                };
            }

            return MapObject(value, mapping =>
            {
                var response = new OpenApiResponse();
                response.Description = GetString(mapping, "description");
                response.Headers = MapDictionary(mapping, "headers", MapReferenceOrHeader);
                response.Content = MapDictionary(mapping, "content", MapMediaType);
                response.Links = MapDictionary(mapping, "links", MapReferenceOrLink);
                PopulateExtensions(response.Extensions, mapping, "description", "headers", "content", "links");
                return response;
            });
        }

        private static OpenApiCallback MapCallback(object value)
        {
            return MapObject(value, mapping =>
            {
                var callback = new OpenApiCallback();
                foreach (var pair in mapping)
                {
                    var key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
                    if (key != null && key.StartsWith("x-", StringComparison.Ordinal))
                    {
                        callback.Extensions[key] = NormalizeValue(pair.Value);
                        continue;
                    }

                    callback.Expressions[key] = MapPathItem(pair.Value);
                }

                return callback;
            });
        }

        private static OpenApiExample MapExample(object value)
        {
            return MapObject(value, mapping =>
            {
                var example = new OpenApiExample();
                example.Summary = GetString(mapping, "summary");
                example.Description = GetString(mapping, "description");
                example.Value = GetValue(mapping, "value");
                example.ExternalValue = GetString(mapping, "externalValue");
                PopulateExtensions(example.Extensions, mapping, "summary", "description", "value", "externalValue");
                return example;
            });
        }

        private static OpenApiLink MapLink(object value)
        {
            return MapObject(value, mapping =>
            {
                var link = new OpenApiLink();
                link.OperationRef = GetString(mapping, "operationRef");
                link.OperationId = GetString(mapping, "operationId");
                link.Parameters = MapRawDictionary(mapping, "parameters");
                link.RequestBody = GetValue(mapping, "requestBody");
                link.Description = GetString(mapping, "description");
                link.Server = MapObject(mapping, "server", MapServer);
                PopulateExtensions(link.Extensions, mapping, "operationRef", "operationId", "parameters", "requestBody", "description", "server");
                return link;
            });
        }

        private static OpenApiComponents MapComponents(object value)
        {
            return MapObject(value, mapping =>
            {
                var components = new OpenApiComponents();
                components.Schemas = MapDictionary(mapping, "schemas", MapSchema);
                components.Responses = MapDictionary(mapping, "responses", MapReferenceOrResponse);
                components.Parameters = MapDictionary(mapping, "parameters", MapReferenceOrParameter);
                components.Examples = MapDictionary(mapping, "examples", MapReferenceOrExample);
                components.RequestBodies = MapDictionary(mapping, "requestBodies", MapReferenceOrRequestBody);
                components.Headers = MapDictionary(mapping, "headers", MapReferenceOrHeader);
                components.SecuritySchemes = MapDictionary(mapping, "securitySchemes", MapReferenceOrSecurityScheme);
                components.Links = MapDictionary(mapping, "links", MapReferenceOrLink);
                components.Callbacks = MapDictionary(mapping, "callbacks", MapReferenceOrCallback);
                components.PathItems = MapDictionary(mapping, "pathItems", MapReferenceOrPathItem);
                PopulateExtensions(components.Extensions, mapping, "schemas", "responses", "parameters", "examples", "requestBodies", "headers", "securitySchemes", "links", "callbacks", "pathItems");
                return components;
            });
        }

        private static OpenApiSecurityScheme MapSecurityScheme(object value)
        {
            return MapObject(value, mapping =>
            {
                var scheme = new OpenApiSecurityScheme();
                scheme.Type = GetString(mapping, "type");
                scheme.Description = GetString(mapping, "description");
                scheme.Name = GetString(mapping, "name");
                scheme.In = GetString(mapping, "in");
                scheme.Scheme = GetString(mapping, "scheme");
                scheme.BearerFormat = GetString(mapping, "bearerFormat");
                scheme.Flows = MapObject(mapping, "flows", MapOAuthFlows);
                scheme.OpenIdConnectUrl = GetString(mapping, "openIdConnectUrl");
                PopulateExtensions(scheme.Extensions, mapping, "type", "description", "name", "in", "scheme", "bearerFormat", "flows", "openIdConnectUrl");
                return scheme;
            });
        }

        private static OpenApiOAuthFlows MapOAuthFlows(object value)
        {
            return MapObject(value, mapping =>
            {
                var flows = new OpenApiOAuthFlows();
                flows.Implicit = MapObject(mapping, "implicit", MapOAuthFlow);
                flows.Password = MapObject(mapping, "password", MapOAuthFlow);
                flows.ClientCredentials = MapObject(mapping, "clientCredentials", MapOAuthFlow);
                flows.AuthorizationCode = MapObject(mapping, "authorizationCode", MapOAuthFlow);
                PopulateExtensions(flows.Extensions, mapping, "implicit", "password", "clientCredentials", "authorizationCode");
                return flows;
            });
        }

        private static OpenApiOAuthFlow MapOAuthFlow(object value)
        {
            return MapObject(value, mapping =>
            {
                var flow = new OpenApiOAuthFlow();
                flow.AuthorizationUrl = GetString(mapping, "authorizationUrl");
                flow.TokenUrl = GetString(mapping, "tokenUrl");
                flow.RefreshUrl = GetString(mapping, "refreshUrl");
                flow.Scopes = MapStringDictionary(mapping, "scopes");
                PopulateExtensions(flow.Extensions, mapping, "authorizationUrl", "tokenUrl", "refreshUrl", "scopes");
                return flow;
            });
        }

        private static OpenApiTag MapTag(object value)
        {
            if (TryConvertToScalarString(value, out var name))
            {
                return new OpenApiTag
                {
                    Name = name,
                };
            }

            return MapObject(value, mapping =>
            {
                var tag = new OpenApiTag();
                tag.Name = GetString(mapping, "name");
                tag.Description = GetString(mapping, "description");
                tag.ExternalDocs = MapObject(mapping, "externalDocs", MapExternalDocumentation);
                PopulateExtensions(tag.Extensions, mapping, "name", "description", "externalDocs");
                return tag;
            });
        }

        private static OpenApiExternalDocumentation MapExternalDocumentation(object value)
        {
            if (TryConvertToScalarString(value, out var url))
            {
                return new OpenApiExternalDocumentation
                {
                    Url = url,
                };
            }

            return MapObject(value, mapping =>
            {
                var externalDocumentation = new OpenApiExternalDocumentation();
                externalDocumentation.Description = GetString(mapping, "description");
                externalDocumentation.Url = GetString(mapping, "url");
                PopulateExtensions(externalDocumentation.Extensions, mapping, "description", "url");
                return externalDocumentation;
            });
        }

        private static OpenApiSchema MapSchema(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (TryConvertToScalarString(value, out var scalarValue))
            {
                return new OpenApiSchema
                {
                    Ref = LooksLikeReference(scalarValue) ? scalarValue : null,
                    Type = LooksLikeReference(scalarValue) ? null : scalarValue,
                };
            }

            return MapObject(value, mapping =>
            {
                var schema = new OpenApiSchema();
                schema.Ref = GetString(mapping, "$ref");
                schema.Schema = GetString(mapping, "$schema");
                schema.DynamicRef = GetString(mapping, "$dynamicRef");
                schema.Vocabulary = MapBooleanDictionary(mapping, "$vocabulary");
                schema.Id = GetString(mapping, "$id");
                schema.Anchor = GetString(mapping, "$anchor");
                schema.DynamicAnchor = GetString(mapping, "$dynamicAnchor");
                schema.Defs = MapDictionary(mapping, "$defs", MapSchema);
                schema.Comment = GetString(mapping, "$comment");
                schema.Type = GetValue(mapping, "type");
                schema.Enum = MapObjectList(mapping, "enum");
                schema.Const = GetValue(mapping, "const");
                schema.MultipleOf = GetNullableDecimal(mapping, "multipleOf");
                schema.Maximum = GetNullableDecimal(mapping, "maximum");
                schema.ExclusiveMaximum = GetNullableDecimal(mapping, "exclusiveMaximum");
                schema.Minimum = GetNullableDecimal(mapping, "minimum");
                schema.ExclusiveMinimum = GetNullableDecimal(mapping, "exclusiveMinimum");
                schema.MaxLength = GetNullableInt32(mapping, "maxLength");
                schema.MinLength = GetNullableInt32(mapping, "minLength");
                schema.Pattern = GetString(mapping, "pattern");
                schema.Items = MapObject(mapping, "items", MapSchema);
                schema.PrefixItems = MapList(mapping, "prefixItems", MapSchema);
                schema.Contains = MapObject(mapping, "contains", MapSchema);
                schema.MaxContains = GetNullableInt32(mapping, "maxContains");
                schema.MinContains = GetNullableInt32(mapping, "minContains");
                schema.MaxItems = GetNullableInt32(mapping, "maxItems");
                schema.MinItems = GetNullableInt32(mapping, "minItems");
                schema.UniqueItems = GetNullableBoolean(mapping, "uniqueItems");
                schema.MaxProperties = GetNullableInt32(mapping, "maxProperties");
                schema.MinProperties = GetNullableInt32(mapping, "minProperties");
                schema.Required = MapScalarList(mapping, "required");
                schema.DependentRequired = MapScalarListDictionary(mapping, "dependentRequired");
                schema.Properties = MapDictionary(mapping, "properties", MapSchema);
                schema.PatternProperties = MapDictionary(mapping, "patternProperties", MapSchema);
                schema.AdditionalProperties = MapBooleanOrSchema(mapping, "additionalProperties");
                schema.PropertyNames = MapObject(mapping, "propertyNames", MapSchema);
                schema.UnevaluatedItems = MapBooleanOrSchema(mapping, "unevaluatedItems");
                schema.UnevaluatedProperties = MapBooleanOrSchema(mapping, "unevaluatedProperties");
                schema.AllOf = MapList(mapping, "allOf", MapSchema);
                schema.AnyOf = MapList(mapping, "anyOf", MapSchema);
                schema.OneOf = MapList(mapping, "oneOf", MapSchema);
                schema.Not = MapObject(mapping, "not", MapSchema);
                schema.If = MapObject(mapping, "if", MapSchema);
                schema.Then = MapObject(mapping, "then", MapSchema);
                schema.Else = MapObject(mapping, "else", MapSchema);
                schema.DependentSchemas = MapDictionary(mapping, "dependentSchemas", MapSchema);
                schema.ContentEncoding = GetString(mapping, "contentEncoding");
                schema.ContentMediaType = GetString(mapping, "contentMediaType");
                schema.ContentSchema = MapObject(mapping, "contentSchema", MapSchema);
                schema.Title = GetString(mapping, "title");
                schema.Description = GetString(mapping, "description");
                schema.Default = GetValue(mapping, "default");
                schema.Deprecated = GetNullableBoolean(mapping, "deprecated");
                schema.ReadOnly = GetNullableBoolean(mapping, "readOnly");
                schema.WriteOnly = GetNullableBoolean(mapping, "writeOnly");
                schema.Examples = MapObjectList(mapping, "examples");
                schema.Format = GetString(mapping, "format");
                schema.Example = GetValue(mapping, "example");
                schema.Discriminator = MapObject(mapping, "discriminator", MapDiscriminator);
                schema.Xml = MapObject(mapping, "xml", MapXml);
                schema.ExternalDocs = MapObject(mapping, "externalDocs", MapExternalDocumentation);
                PopulateExtensions(schema.Extensions, mapping, "$ref", "$schema", "$dynamicRef", "$vocabulary", "$id", "$anchor", "$dynamicAnchor", "$defs", "$comment", "type", "enum", "const", "multipleOf", "maximum", "exclusiveMaximum", "minimum", "exclusiveMinimum", "maxLength", "minLength", "pattern", "items", "prefixItems", "contains", "maxContains", "minContains", "maxItems", "minItems", "uniqueItems", "maxProperties", "minProperties", "required", "dependentRequired", "properties", "patternProperties", "additionalProperties", "propertyNames", "unevaluatedItems", "unevaluatedProperties", "allOf", "anyOf", "oneOf", "not", "if", "then", "else", "dependentSchemas", "contentEncoding", "contentMediaType", "contentSchema", "title", "description", "default", "deprecated", "readOnly", "writeOnly", "examples", "format", "example", "discriminator", "xml", "externalDocs");
                PopulateUnrecognizedKeywords(schema.UnrecognizedKeywords, mapping, schema.Extensions.Keys);
                return schema;
            });
        }

        private static OpenApiDiscriminator MapDiscriminator(object value)
        {
            return MapObject(value, mapping =>
            {
                var discriminator = new OpenApiDiscriminator();
                discriminator.PropertyName = GetString(mapping, "propertyName");
                discriminator.Mapping = MapStringDictionary(mapping, "mapping");
                PopulateExtensions(discriminator.Extensions, mapping, "propertyName", "mapping");
                return discriminator;
            });
        }

        private static OpenApiXml MapXml(object value)
        {
            return MapObject(value, mapping =>
            {
                var xml = new OpenApiXml();
                xml.Name = GetString(mapping, "name");
                xml.Namespace = GetString(mapping, "namespace");
                xml.Prefix = GetString(mapping, "prefix");
                xml.Attribute = GetNullableBoolean(mapping, "attribute");
                xml.Wrapped = GetNullableBoolean(mapping, "wrapped");
                PopulateExtensions(xml.Extensions, mapping, "name", "namespace", "prefix", "attribute", "wrapped");
                return xml;
            });
        }

        private static OpenApiReferenceOr<OpenApiPathItem> MapReferenceOrPathItem(object value)
        {
            return MapReferenceOr(value, MapPathItem);
        }

        private static OpenApiReferenceOr<OpenApiParameter> MapReferenceOrParameter(object value)
        {
            return MapReferenceOr(value, MapParameter);
        }

        private static OpenApiReferenceOr<OpenApiRequestBody> MapReferenceOrRequestBody(object value)
        {
            return MapReferenceOr(value, MapRequestBody);
        }

        private static OpenApiReferenceOr<OpenApiResponse> MapReferenceOrResponse(object value)
        {
            return MapReferenceOr(value, MapResponse);
        }

        private static OpenApiReferenceOr<OpenApiExample> MapReferenceOrExample(object value)
        {
            return MapReferenceOr(value, MapExample);
        }

        private static OpenApiReferenceOr<OpenApiHeader> MapReferenceOrHeader(object value)
        {
            return MapReferenceOr(value, MapHeader);
        }

        private static OpenApiReferenceOr<OpenApiSecurityScheme> MapReferenceOrSecurityScheme(object value)
        {
            return MapReferenceOr(value, MapSecurityScheme);
        }

        private static OpenApiReferenceOr<OpenApiLink> MapReferenceOrLink(object value)
        {
            return MapReferenceOr(value, MapLink);
        }

        private static OpenApiReferenceOr<OpenApiCallback> MapReferenceOrCallback(object value)
        {
            return MapReferenceOr(value, MapCallback);
        }

        private static OpenApiReferenceOr<T> MapReferenceOr<T>(object value, Func<object, T> mapValue)
            where T : class
        {
            if (TryConvertToScalarString(value, out var referenceValue) && LooksLikeReference(referenceValue))
            {
                return new OpenApiReferenceOr<T>
                {
                    Reference = new OpenApiReference
                    {
                        Ref = referenceValue,
                    },
                };
            }

            if (!(value is IDictionary<object, object> mapping))
            {
                return new OpenApiReferenceOr<T>
                {
                    Value = mapValue(value),
                };
            }

            if (mapping.ContainsKey("$ref"))
            {
                return new OpenApiReferenceOr<T>
                {
                    Reference = MapReference(value),
                };
            }

            return new OpenApiReferenceOr<T>
            {
                Value = mapValue(value),
            };
        }

        private static OpenApiReference MapReference(object value)
        {
            return MapObject(value, mapping =>
            {
                var reference = new OpenApiReference();
                reference.Ref = GetString(mapping, "$ref");
                reference.Summary = GetString(mapping, "summary");
                reference.Description = GetString(mapping, "description");
                PopulateExtensions(reference.Extensions, mapping, "$ref", "summary", "description");
                return reference;
            });
        }

        private static OpenApiSecurityRequirement MapSecurityRequirement(object value)
        {
            if (TryConvertToScalarString(value, out var schemeName) && !string.IsNullOrWhiteSpace(schemeName))
            {
                var requirement = new OpenApiSecurityRequirement();
                requirement.Requirements[schemeName] = new List<string>();
                return requirement;
            }

            return MapObject(value, mapping =>
            {
                var requirement = new OpenApiSecurityRequirement();
                foreach (var pair in mapping)
                {
                    var key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
                    if (key != null && key.StartsWith("x-", StringComparison.Ordinal))
                    {
                        requirement.Extensions[key] = NormalizeValue(pair.Value);
                        continue;
                    }

                    requirement.Requirements[key] = MapScalarSequence(pair.Value);
                }

                return requirement;
            });
        }

        private static T MapObject<T>(IDictionary<object, object> mapping, string key, Func<object, T> mapper)
            where T : class
        {
            return TryGetValue(mapping, key, out var value) ? mapper(value) : null;
        }

        private static T MapObject<T>(object value, Func<IDictionary<object, object>, T> mapper)
            where T : class
        {
            if (value == null)
            {
                return null;
            }

            if (TryConvertToMapping(value, out var mapping))
            {
                return mapper(mapping);
            }

            throw new InvalidOperationException("Expected a mapping.");
        }

        private static IList<T> MapList<T>(IDictionary<object, object> mapping, string key, Func<object, T> mapper)
        {
            return TryGetValue(mapping, key, out var value) ? MapList(value, mapper) : new List<T>();
        }

        private static IList<T> MapList<T>(object value, Func<object, T> mapper)
        {
            var list = new List<T>();

            if (value == null)
            {
                return list;
            }

            if (value is IDictionary<object, object> || value is string || !(value is IEnumerable sequence))
            {
                list.Add(mapper(value));
                return list;
            }

            foreach (var item in sequence)
            {
                list.Add(mapper(item));
            }

            return list;
        }

        private static IDictionary<string, T> MapDictionary<T>(IDictionary<object, object> mapping, string key, Func<object, T> mapper)
        {
            return TryGetValue(mapping, key, out var value) ? MapDictionary(value, mapper) : new Dictionary<string, T>();
        }

        private static IDictionary<string, T> MapDictionary<T>(object value, Func<object, T> mapper)
        {
            var result = new Dictionary<string, T>();

            if (value == null)
            {
                return result;
            }

            if (!TryConvertToMapping(value, out var mapping))
            {
                throw new InvalidOperationException("Expected a mapping.");
            }

            foreach (var pair in mapping)
            {
                result[Convert.ToString(pair.Key, CultureInfo.InvariantCulture)] = mapper(pair.Value);
            }

            return result;
        }

        private static IDictionary<string, object> MapRawDictionary(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? MapRawDictionary(value) : new Dictionary<string, object>();
        }

        private static IDictionary<string, object> MapRawDictionary(object value)
        {
            var result = new Dictionary<string, object>();

            if (value == null)
            {
                return result;
            }

            if (!TryConvertToMapping(value, out var mapping))
            {
                throw new InvalidOperationException("Expected a mapping.");
            }

            foreach (var pair in mapping)
            {
                result[Convert.ToString(pair.Key, CultureInfo.InvariantCulture)] = NormalizeValue(pair.Value);
            }

            return result;
        }

        private static IDictionary<string, string> MapStringDictionary(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? MapStringDictionary(value) : new Dictionary<string, string>();
        }

        private static IDictionary<string, string> MapStringDictionary(object value)
        {
            var result = new Dictionary<string, string>();

            if (value == null)
            {
                return result;
            }

            if (!TryConvertToMapping(value, out var mapping))
            {
                throw new InvalidOperationException("Expected a mapping.");
            }

            foreach (var pair in mapping)
            {
                var itemValue = ConvertToString(pair.Value);
                if (itemValue != null)
                {
                    result[Convert.ToString(pair.Key, CultureInfo.InvariantCulture)] = itemValue;
                }
            }

            return result;
        }

        private static IDictionary<string, bool> MapBooleanDictionary(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? MapBooleanDictionary(value) : new Dictionary<string, bool>();
        }

        private static IDictionary<string, bool> MapBooleanDictionary(object value)
        {
            var result = new Dictionary<string, bool>();

            if (value == null)
            {
                return result;
            }

            if (!TryConvertToMapping(value, out var mapping))
            {
                throw new InvalidOperationException("Expected a mapping.");
            }

            foreach (var pair in mapping)
            {
                var booleanValue = ToNullableBoolean(pair.Value);
                if (booleanValue.HasValue)
                {
                    result[Convert.ToString(pair.Key, CultureInfo.InvariantCulture)] = booleanValue.Value;
                }
            }

            return result;
        }

        private static IDictionary<string, IList<string>> MapScalarListDictionary(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? MapScalarListDictionary(value) : new Dictionary<string, IList<string>>();
        }

        private static IDictionary<string, IList<string>> MapScalarListDictionary(object value)
        {
            var result = new Dictionary<string, IList<string>>();

            if (value == null)
            {
                return result;
            }

            if (!TryConvertToMapping(value, out var mapping))
            {
                throw new InvalidOperationException("Expected a mapping.");
            }

            foreach (var pair in mapping)
            {
                result[Convert.ToString(pair.Key, CultureInfo.InvariantCulture)] = MapScalarSequence(pair.Value);
            }

            return result;
        }

        private static IList<string> MapScalarList(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? MapScalarSequence(value) : new List<string>();
        }

        private static IList<string> MapScalarSequence(object value)
        {
            var result = new List<string>();

            if (value == null)
            {
                return result;
            }

            if (value is IDictionary<object, object>)
            {
                throw new InvalidOperationException("Expected a scalar or sequence of scalars.");
            }

            if (value is string || !(value is IEnumerable sequence))
            {
                var scalar = ConvertToString(value);
                if (scalar != null)
                {
                    result.Add(scalar);
                }

                return result;
            }

            foreach (var item in sequence)
            {
                var scalar = ConvertToString(item);
                if (scalar == null)
                {
                    throw new InvalidOperationException("Expected a scalar or sequence of scalars.");
                }

                result.Add(scalar);
            }

            return result;
        }

        private static IList<object> MapObjectList(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? MapObjectList(value) : new List<object>();
        }

        private static IList<object> MapObjectList(object value)
        {
            var result = new List<object>();

            if (value == null)
            {
                return result;
            }

            if (value is string || value is IDictionary<object, object> || !(value is IEnumerable sequence))
            {
                result.Add(NormalizeValue(value));
                return result;
            }

            foreach (var item in sequence)
            {
                result.Add(NormalizeValue(item));
            }

            return result;
        }

        private static OpenApiBooleanOrSchema MapBooleanOrSchema(IDictionary<object, object> mapping, string key)
        {
            if (!TryGetValue(mapping, key, out var value))
            {
                return null;
            }

            var booleanValue = ToNullableBoolean(value);
            if (booleanValue.HasValue)
            {
                return new OpenApiBooleanOrSchema
                {
                    Boolean = booleanValue,
                };
            }

            return new OpenApiBooleanOrSchema
            {
                Schema = MapSchema(value),
            };
        }

        private static string GetString(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? ConvertToString(value) : null;
        }

        private static object GetValue(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? NormalizeValue(value) : null;
        }

        private static bool? GetNullableBoolean(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? ToNullableBoolean(value) : null;
        }

        private static int? GetNullableInt32(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? ToNullableInt32(value) : null;
        }

        private static decimal? GetNullableDecimal(IDictionary<object, object> mapping, string key)
        {
            return TryGetValue(mapping, key, out var value) ? ToNullableDecimal(value) : null;
        }

        private static void PopulateExtensions(IDictionary<string, object> target, IDictionary<object, object> source, params string[] knownKeys)
        {
            var known = new HashSet<string>(knownKeys, StringComparer.Ordinal);
            foreach (var pair in source)
            {
                var key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
                if (key != null && key.StartsWith("x-", StringComparison.Ordinal) && !known.Contains(key))
                {
                    target[key] = NormalizeValue(pair.Value);
                }
            }
        }

        private static void PopulateUnrecognizedKeywords(IDictionary<string, object> target, IDictionary<object, object> source, IEnumerable<string> extensionKeys)
        {
            var extensionKeySet = new HashSet<string>(StringComparer.Ordinal);
            if (extensionKeys != null)
            {
                foreach (var extensionKey in extensionKeys)
                {
                    extensionKeySet.Add(extensionKey);
                }
            }

            foreach (var pair in source)
            {
                var key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(key) || extensionKeySet.Contains(key))
                {
                    continue;
                }

                if (IsKnownSchemaKeyword(key))
                {
                    continue;
                }

                target[key] = NormalizeValue(pair.Value);
            }
        }

        private static bool IsKnownSchemaKeyword(string key)
        {
            switch (key)
            {
                case "$ref":
                case "$schema":
                case "$dynamicRef":
                case "$vocabulary":
                case "$id":
                case "$anchor":
                case "$dynamicAnchor":
                case "$defs":
                case "$comment":
                case "type":
                case "enum":
                case "const":
                case "multipleOf":
                case "maximum":
                case "exclusiveMaximum":
                case "minimum":
                case "exclusiveMinimum":
                case "maxLength":
                case "minLength":
                case "pattern":
                case "items":
                case "prefixItems":
                case "contains":
                case "maxContains":
                case "minContains":
                case "maxItems":
                case "minItems":
                case "uniqueItems":
                case "maxProperties":
                case "minProperties":
                case "required":
                case "dependentRequired":
                case "properties":
                case "patternProperties":
                case "additionalProperties":
                case "propertyNames":
                case "unevaluatedItems":
                case "unevaluatedProperties":
                case "allOf":
                case "anyOf":
                case "oneOf":
                case "not":
                case "if":
                case "then":
                case "else":
                case "dependentSchemas":
                case "contentEncoding":
                case "contentMediaType":
                case "contentSchema":
                case "title":
                case "description":
                case "default":
                case "deprecated":
                case "readOnly":
                case "writeOnly":
                case "examples":
                case "format":
                case "example":
                case "discriminator":
                case "xml":
                case "externalDocs":
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetValue(IDictionary<object, object> mapping, string key, out object value)
        {
            if (mapping.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var pair in mapping)
            {
                if (string.Equals(Convert.ToString(pair.Key, CultureInfo.InvariantCulture), key, StringComparison.Ordinal))
                {
                    value = pair.Value;
                    return true;
                }
            }

            foreach (var pair in mapping)
            {
                if (string.Equals(Convert.ToString(pair.Key, CultureInfo.InvariantCulture), key, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static object NormalizeValue(object value)
        {
            if (value is IDictionary<object, object> mapping)
            {
                var result = new Dictionary<string, object>();
                foreach (var pair in mapping)
                {
                    result[Convert.ToString(pair.Key, CultureInfo.InvariantCulture)] = NormalizeValue(pair.Value);
                }

                return result;
            }

            if (!(value is string) && value is IEnumerable sequence)
            {
                var list = new List<object>();
                foreach (var item in sequence)
                {
                    list.Add(NormalizeValue(item));
                }

                return list;
            }

            return value;
        }

        private static string ConvertToString(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (TryConvertToScalarString(value, out var stringValue))
            {
                return stringValue;
            }

            return null;
        }

        private static bool? ToNullableBoolean(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is bool boolean)
            {
                return boolean;
            }

            var stringValue = ConvertToString(value);
            if (stringValue == null)
            {
                return null;
            }

            if (bool.TryParse(stringValue, out var parsed))
            {
                return parsed;
            }

            switch (stringValue.Trim().ToLowerInvariant())
            {
                case "yes":
                case "on":
                case "1":
                    return true;
                case "no":
                case "off":
                case "0":
                    return false;
                default:
                    return null;
            }
        }

        private static int? ToNullableInt32(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int int32)
            {
                return int32;
            }

            if (value is long int64)
            {
                return checked((int)int64);
            }

            if (value is short int16)
            {
                return int16;
            }

            if (value is byte @byte)
            {
                return @byte;
            }

            return int.TryParse(ConvertToString(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : (int?)null;
        }

        private static decimal? ToNullableDecimal(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is decimal @decimal)
            {
                return @decimal;
            }

            if (value is double @double)
            {
                return (decimal)@double;
            }

            if (value is float @float)
            {
                return (decimal)@float;
            }

            if (value is int int32)
            {
                return int32;
            }

            if (value is long int64)
            {
                return int64;
            }

            return decimal.TryParse(ConvertToString(value), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : (decimal?)null;
        }

        private static bool TryConvertToMapping(object value, out IDictionary<object, object> mapping)
        {
            if (value is IDictionary<object, object> objectMapping)
            {
                mapping = objectMapping;
                return true;
            }

            if (!(value is string) && value is IEnumerable sequence)
            {
                var items = new List<object>();
                foreach (var item in sequence)
                {
                    items.Add(item);
                }

                if (items.Count == 1 && items[0] is IDictionary<object, object> singleMapping)
                {
                    mapping = singleMapping;
                    return true;
                }

                if (items.Count > 0)
                {
                    var merged = new Dictionary<object, object>();
                    foreach (var item in items)
                    {
                        if (!(item is IDictionary<object, object> itemMapping) || itemMapping.Count != 1)
                        {
                            mapping = null;
                            return false;
                        }

                        foreach (var pair in itemMapping)
                        {
                            merged[pair.Key] = pair.Value;
                        }
                    }

                    mapping = merged;
                    return true;
                }
            }

            mapping = null;
            return false;
        }

        private static bool TryConvertToScalarString(object value, out string stringValue)
        {
            if (value == null)
            {
                stringValue = null;
                return false;
            }

            if (value is IEnumerable sequence && !(value is string))
            {
                if (TryConvertToMapping(value, out _))
                {
                    stringValue = null;
                    return false;
                }

                var items = new List<object>();
                foreach (var item in sequence)
                {
                    items.Add(item);
                }

                if (items.Count == 1)
                {
                    return TryConvertToScalarString(items[0], out stringValue);
                }

                stringValue = null;
                return false;
            }

            stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool LooksLikeReference(string value)
        {
            return
                !string.IsNullOrWhiteSpace(value) &&
                (value.StartsWith("#/", StringComparison.Ordinal) ||
                 value.IndexOf("#/", StringComparison.Ordinal) >= 0 ||
                 value.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                 value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                 value.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        }
    }
}
