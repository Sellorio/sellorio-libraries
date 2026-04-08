using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ExtensionMethodsShouldNotIndicateNullAcceptanceAnalyzer : AnalyzerBase<ExtensionMethodsShouldNotIndicateNullAcceptanceAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0023;

        private static readonly string[] ProhibitedPrefixes = new[]
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
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Check if this is an extension method
            if (!IsExtensionMethod(methodDeclaration))
            {
                return;
            }

            var methodName = methodDeclaration.Identifier.Text;

            // Check if the method name starts with any prohibited prefix
            foreach (var prefix in ProhibitedPrefixes)
            {
                if (methodName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        methodDeclaration.Identifier.GetLocation(),
                        methodName);

                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }

        private bool IsExtensionMethod(MethodDeclarationSyntax methodDeclaration)
        {
            // Extension methods must be static
            if (!methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                return false;
            }

            // Extension methods must have at least one parameter
            if (methodDeclaration.ParameterList.Parameters.Count == 0)
            {
                return false;
            }

            // The first parameter must have the 'this' modifier
            var firstParameter = methodDeclaration.ParameterList.Parameters[0];
            return firstParameter.Modifiers.Any(m => m.IsKind(SyntaxKind.ThisKeyword));
        }
    }
}
