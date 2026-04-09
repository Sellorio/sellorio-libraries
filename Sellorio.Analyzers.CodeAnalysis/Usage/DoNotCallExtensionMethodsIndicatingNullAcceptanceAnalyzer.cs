using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DoNotCallExtensionMethodsIndicatingNullAcceptanceAnalyzer : AnalyzerBase<DoNotCallExtensionMethodsIndicatingNullAcceptanceAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0024;

        private static readonly string[] _prohibitedPrefixes = new[]
        {
            "IsNull",
            "WhenNull",
            "IfNull",
            "OrNull",
            "WithNull",
            "ForNull"
        };

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Get the symbol info for the invoked method
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            // Only check extension methods
            if (!methodSymbol.IsExtensionMethod)
            {
                return;
            }

            var methodName = methodSymbol.Name;

            // Check if the method name starts with any prohibited prefix
            foreach (var prefix in _prohibitedPrefixes)
            {
                if (methodName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var location = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                        ? memberAccess.Name.GetLocation()
                        : invocation.Expression is IdentifierNameSyntax identifier
                            ? identifier.GetLocation()
                            : invocation.GetLocation();

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        location,
                        methodName);

                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }
    }
}
