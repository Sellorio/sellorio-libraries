using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseAnyInsteadOfCountOrLengthAnalyzer : AnalyzerBase<UseAnyInsteadOfCountOrLengthAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0007;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.GreaterThanExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression);
        }

        private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            var binaryExpr = (BinaryExpressionSyntax)context.Node;

            // Check for "something > 0", "!= 0", "== 0"
            if (!IsZeroLiteral(binaryExpr.Right) && !IsZeroLiteral(binaryExpr.Left))
            {
                return;
            }

            var checkedSide =
                IsZeroLiteral(binaryExpr.Left)
                    ? binaryExpr.Right
                    : binaryExpr.Left;

            // Case 1: Enumerable.Count()
            if (checkedSide is InvocationExpressionSyntax invocation)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                if (symbol == null)
                {
                    return;
                }

                if (symbol.Name == "Count" &&
                    symbol.ContainingNamespace.ToDisplayString() == "System.Linq")
                {
                    // handled by CA1827
                    return;
                }
            }

            // Case 2: .Length or .Count property
            if (checkedSide is MemberAccessExpressionSyntax memberAccess)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;

                if (symbol is IPropertySymbol property &&
                    (property.Name == "Length" || property.Name == "Count"))
                {
                    var collectionType = context.SemanticModel.GetTypeInfo(memberAccess.Expression);

                    if (collectionType.Type?.SpecialType == SpecialType.System_String)
                    {
                        return;
                    }

                    Report(context, memberAccess, property.Name);
                }
            }
        }

        private static bool IsZeroLiteral(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax literal &&
                   literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
                   literal.Token.ValueText == "0";
        }

        private void Report(SyntaxNodeAnalysisContext context, SyntaxNode node, string memberName)
        {
            var diagnostic =
                Diagnostic.Create(
                    DiagnosticDescriptor,
                    node.GetLocation(),
                    memberName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
