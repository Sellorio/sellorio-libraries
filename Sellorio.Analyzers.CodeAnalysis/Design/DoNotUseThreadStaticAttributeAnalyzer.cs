using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DoNotUseThreadStaticAttributeAnalyzer : AnalyzerBase<DoNotUseThreadStaticAttributeAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0022;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attribute = (AttributeSyntax)context.Node;

            // Get the symbol info for the attribute
            var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
            var attributeConstructor = symbolInfo.Symbol as IMethodSymbol;
            if (attributeConstructor == null)
            {
                return;
            }

            var attributeClass = attributeConstructor.ContainingType;
            if (attributeClass == null)
            {
                return;
            }

            // Check if this is the ThreadStaticAttribute
            if (attributeClass.Name == "ThreadStaticAttribute" &&
                attributeClass.ContainingNamespace?.ToDisplayString() == "System")
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    attribute.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
