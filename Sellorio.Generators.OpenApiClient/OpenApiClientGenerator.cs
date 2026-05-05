using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Generators.OpenApiClient.CodeGeneration;
using Sellorio.Generators.OpenApiCommon.Parser;

namespace Sellorio.Generators.OpenApiClient
{
    [Generator(LanguageNames.CSharp)]
    public sealed class OpenApiClientGenerator : IIncrementalGenerator
    {
        private const string AttributeFullName = "Sellorio.Generators.OpenApiClient.GenerateOpenApiClientAttribute";

        private const string AttributeSource = @"using System;

namespace Sellorio.Generators.OpenApiClient;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class GenerateOpenApiClientAttribute : Attribute
{
    public GenerateOpenApiClientAttribute(string openApiDefinitionFilename)
    {
        OpenApiDefinitionFilename = openApiDefinitionFilename;
    }

    public string OpenApiDefinitionFilename { get; }

    public bool GenerateImplementation { get; set; } = true;

    public string[] IncludedTags { get; set; } = Array.Empty<string>();
}
";

        static OpenApiClientGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var assemblyName = new AssemblyName(args.Name).Name;

                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    return null;
                }

                var assemblyDirectory = Path.GetDirectoryName(typeof(OpenApiClientGenerator).Assembly.Location);

                if (string.IsNullOrWhiteSpace(assemblyDirectory))
                {
                    return null;
                }

                var assemblyPath = Path.Combine(assemblyDirectory, assemblyName + ".dll");

                try
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
                catch
                {
                    return null;
                }
            };
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(generationContext =>
            {
                generationContext.AddSource(
                    "GenerateOpenApiClientAttribute.g.cs",
                    SourceText.From(AttributeSource, Encoding.UTF8));
            });

            var generationTargets = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeFullName,
                    (node, _) => node is InterfaceDeclarationSyntax,
                    (syntaxContext, _) => GetGenerationTarget(syntaxContext))
                .Where(target => target != null);

            var additionalFiles = context.AdditionalTextsProvider
                .Select((additionalText, cancellationToken) =>
                    new AdditionalFileData(additionalText.Path, additionalText.GetText(cancellationToken)?.ToString()))
                .Collect();

            var generationInputs = generationTargets.Combine(additionalFiles);

            context.RegisterSourceOutput(generationInputs, (productionContext, generationInput) =>
            {
                var generationTarget = generationInput.Left;
                var definitionFile = ReadOpenApiDefinition(generationTarget, generationInput.Right);

                if (!definitionFile.Exists)
                {
                    GenerateOpenApiClientErrorInterface(
                        productionContext,
                        generationTarget,
                        definitionFile.ErrorMessage);

                    return;
                }

                try
                {
                    var document = OpenApiParser.ParseYaml(definitionFile.Contents);
                    var generatedCode = OpenApiClientCodeGenerator.Generate(generationTarget, document);
                    var namespacePrefix = string.IsNullOrWhiteSpace(generationTarget.NamespaceName)
                        ? "Global"
                        : generationTarget.NamespaceName.Replace('.', '_');

                    productionContext.AddSource(
                        $"{namespacePrefix}_{generationTarget.InterfaceName}.IClient.g.cs",
                        SourceText.From(generatedCode.InterfaceSource, Encoding.UTF8));

                    if (generatedCode.ImplementationSource != null)
                    {
                        productionContext.AddSource(
                            $"{namespacePrefix}_{generationTarget.ClassName}.Client.g.cs",
                            SourceText.From(generatedCode.ImplementationSource, Encoding.UTF8));
                    }
                }
                catch (Exception exception)
                {
                    GenerateOpenApiClientErrorInterface(
                        productionContext,
                        generationTarget,
                        "OpenAPI client generation failed: " + exception.Message.Replace("\r", " ").Replace("\n", " "));
                }
            });
        }

        private static OpenApiClientGenerationTarget GetGenerationTarget(GeneratorAttributeSyntaxContext context)
        {
            if (!(context.TargetSymbol is INamedTypeSymbol interfaceSymbol) ||
                interfaceSymbol.TypeKind != TypeKind.Interface ||
                interfaceSymbol.ContainingType != null ||
                interfaceSymbol.TypeParameters.Length > 0)
            {
                return null;
            }

            if (!(context.TargetNode is InterfaceDeclarationSyntax declaration) ||
                !declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                return null;
            }

            var attribute = context.Attributes.FirstOrDefault();
            var openApiDefinitionFilename =
                attribute?.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string value
                    ? value
                    : null;

            if (string.IsNullOrWhiteSpace(openApiDefinitionFilename))
            {
                return null;
            }

            var generateImplementation = true;
            var includedTags = ImmutableArray<string>.Empty;

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "GenerateImplementation" && namedArgument.Value.Value is bool boolValue)
                {
                    generateImplementation = boolValue;
                }
                else if (namedArgument.Key == "IncludedTags" && !namedArgument.Value.IsNull)
                {
                    includedTags = namedArgument.Value.Values
                        .Where(argument => argument.Value is string)
                        .Select(argument => (string)argument.Value)
                        .Where(argument => !string.IsNullOrWhiteSpace(argument))
                        .ToImmutableArray();
                }
            }

            return new OpenApiClientGenerationTarget(
                interfaceSymbol.ContainingNamespace.IsGlobalNamespace
                    ? null
                    : interfaceSymbol.ContainingNamespace.ToDisplayString(),
                GetAccessibility(interfaceSymbol.DeclaredAccessibility),
                interfaceSymbol.Name,
                GetClassName(interfaceSymbol.Name),
                openApiDefinitionFilename,
                generateImplementation,
                includedTags);
        }

        private static void GenerateOpenApiClientErrorInterface(
            SourceProductionContext context,
            OpenApiClientGenerationTarget generationTarget,
            string errorMessage)
        {
            var namespacePrefix = string.IsNullOrWhiteSpace(generationTarget.NamespaceName)
                ? "Global"
                : generationTarget.NamespaceName.Replace('.', '_');

            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(generationTarget.NamespaceName))
            {
                builder.Append("namespace ");
                builder.Append(generationTarget.NamespaceName);
                builder.AppendLine(";");
                builder.AppendLine();
            }

            builder.Append("#error ");
            builder.AppendLine(errorMessage);
            builder.AppendLine();
            builder.Append(generationTarget.InterfaceAccessibility);
            builder.Append(" partial interface ");
            builder.Append(generationTarget.InterfaceName);
            builder.AppendLine();
            builder.AppendLine("{");
            builder.AppendLine("}");
            builder.AppendLine();

            context.AddSource(
                $"{namespacePrefix}_{generationTarget.InterfaceName}.IClient.g.cs",
                SourceText.From(builder.ToString(), Encoding.UTF8));
        }

        private static DefinitionFileResult ReadOpenApiDefinition(
            OpenApiClientGenerationTarget generationTarget,
            ImmutableArray<AdditionalFileData> additionalFiles)
        {
            var normalizedPath = NormalizePath(generationTarget.OpenApiDefinitionFilename);

            foreach (var additionalFile in additionalFiles)
            {
                if (!PathMatches(additionalFile.Path, normalizedPath))
                {
                    continue;
                }

                if (additionalFile.Contents is null)
                {
                    return DefinitionFileResult.Missing(
                        $"Unable to read OpenAPI definition file '{generationTarget.OpenApiDefinitionFilename}'.");
                }

                return DefinitionFileResult.Found(additionalFile.Contents);
            }

            return DefinitionFileResult.Missing(
                $"OpenAPI definition file '{generationTarget.OpenApiDefinitionFilename}' was not found.");
        }

        private static bool PathMatches(string candidatePath, string expectedPath)
        {
            var normalizedCandidatePath = NormalizePath(candidatePath);

            return
                string.Equals(normalizedCandidatePath, expectedPath, StringComparison.OrdinalIgnoreCase) ||
                normalizedCandidatePath.EndsWith("/" + expectedPath, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string GetClassName(string interfaceName)
        {
            return
                interfaceName.Length > 1 && interfaceName[0] == 'I' && char.IsUpper(interfaceName[1])
                    ? interfaceName.Substring(1)
                    : interfaceName + "Client";
        }

        private static string GetAccessibility(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.NotApplicable: return "internal";
                case Accessibility.Public: return "public";
                case Accessibility.Private: return "private";
                case Accessibility.Protected: return "protected";
                case Accessibility.Internal: return "internal";
                case Accessibility.ProtectedAndInternal: return "private protected";
                case Accessibility.ProtectedOrInternal: return "protected internal";
                default: return "internal";
            }
        }

        private sealed class AdditionalFileData
        {
            public string Path { get; }
            public string Contents { get; }

            public AdditionalFileData(string path, string contents)
            {
                Path = path;
                Contents = contents;
            }
        }

        private sealed class DefinitionFileResult
        {
            public bool Exists { get; }
            public string Contents { get; }
            public string ErrorMessage { get; }

            public DefinitionFileResult(bool exists, string contents, string errorMessage)
            {
                Exists = exists;
                Contents = contents;
                ErrorMessage = errorMessage;
            }

            public static DefinitionFileResult Found(string contents)
            {
                return new DefinitionFileResult(true, contents, null);
            }

            public static DefinitionFileResult Missing(string errorMessage)
            {
                return new DefinitionFileResult(false, null, errorMessage);
            }
        }
    }
}
