using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseHasValueAnalyzer : AnalyzerBase<DoNotUseHasValueAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0010;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Check the member name is "HasValue"
            if (memberAccess.Name.Identifier.Text != "HasValue")
            {
                return;
            }

            // Get symbol info
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IPropertySymbol;

            if (symbol == null ||
                symbol.Name != "HasValue" ||
                symbol.ContainingType == null ||
                symbol.ContainingType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            {
                return;
            }

            // Report diagnostic
            var diagnostic = Diagnostic.Create(DiagnosticDescriptor, memberAccess.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
