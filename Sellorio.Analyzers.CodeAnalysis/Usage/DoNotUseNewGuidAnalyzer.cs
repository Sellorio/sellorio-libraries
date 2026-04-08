using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseNewGuidAnalyzer : AnalyzerBase<DoNotUseNewGuidAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0031;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count != 0)
            {
                return;
            }

            var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;

            if (typeSymbol == null || typeSymbol.ToDisplayString() != "System.Guid")
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, objectCreation.GetLocation()));
        }

        private void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;

            if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count != 0)
            {
                return;
            }

            var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;

            if (typeSymbol == null || typeSymbol.ToDisplayString() != "System.Guid")
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, objectCreation.GetLocation()));
        }
    }
}
