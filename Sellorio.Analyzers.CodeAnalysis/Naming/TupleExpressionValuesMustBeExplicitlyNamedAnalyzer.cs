using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TupleExpressionValuesMustBeExplicitlyNamedAnalyzer : AnalyzerBase<TupleExpressionValuesMustBeExplicitlyNamedAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0016;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeTupleExpression, SyntaxKind.TupleExpression);
        }

        private void AnalyzeTupleExpression(SyntaxNodeAnalysisContext context)
        {
            var tuple = (TupleExpressionSyntax)context.Node;

            // Check if the tuple expression has a target type with explicit names
            if (HasExplicitlyNamedTargetType(context, tuple))
            {
                return;
            }

            foreach (var argument in tuple.Arguments)
            {
                // If NameColon is null, it's implicitly named
                if (argument.NameColon == null)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        argument.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool HasExplicitlyNamedTargetType(SyntaxNodeAnalysisContext context, TupleExpressionSyntax tuple)
        {
            var typeInfo = context.SemanticModel.GetTypeInfo(tuple);
            var type = typeInfo.Type;
            var convertedType = typeInfo.ConvertedType;

            // If there's no conversion or types are the same, names are inferred from the expression itself
            if (convertedType == null || SymbolEqualityComparer.Default.Equals(type, convertedType))
            {
                return false;
            }

            if (convertedType.IsTupleType)
            {
                var namedTypeSymbol = convertedType as INamedTypeSymbol;
                if (namedTypeSymbol != null && namedTypeSymbol.TupleElements != null)
                {
                    // Check if any element has an explicitly provided name (not Item1, Item2, etc.)
                    foreach (var element in namedTypeSymbol.TupleElements)
                    {
                        if (!string.IsNullOrEmpty(element.Name) && !element.Name.StartsWith("Item"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
