using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArithmeticOperatorsMustBeTrailingAnalyzer : AnalyzerBase<ArithmeticOperatorsMustBeTrailingAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0009;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                AnalyzeExpression,
                SyntaxKind.AddExpression,
                SyntaxKind.SubtractExpression,
                SyntaxKind.MultiplyExpression,
                SyntaxKind.DivideExpression);
        }

        private void AnalyzeExpression(SyntaxNodeAnalysisContext context)
        {
            var binary = (BinaryExpressionSyntax)context.Node;

            var operatorToken = binary.OperatorToken;

            var tree = binary.SyntaxTree;
            var text = tree.GetText(context.CancellationToken);

            var operatorLine = text.Lines.GetLineFromPosition(operatorToken.SpanStart);
            var leftLine = text.Lines.GetLineFromPosition(binary.Left.Span.End);

            // Trigger when operator starts on a new line
            if (operatorLine.LineNumber > leftLine.LineNumber)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptor,
                        operatorToken.GetLocation()));
            }
        }
    }
}
