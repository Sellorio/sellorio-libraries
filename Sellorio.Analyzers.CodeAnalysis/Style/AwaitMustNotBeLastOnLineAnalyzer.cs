using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitMustNotBeLastOnLineAnalyzer : AnalyzerBase<AwaitMustNotBeLastOnLineAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0030;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
        }

        private void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
        {
            var awaitExpression = (AwaitExpressionSyntax)context.Node;
            var awaitKeyword = awaitExpression.AwaitKeyword;
            var awaitedExpression = awaitExpression.Expression;

            var text = awaitExpression.SyntaxTree.GetText(context.CancellationToken);
            var awaitLine = text.Lines.GetLineFromPosition(awaitKeyword.SpanStart).LineNumber;
            var expressionLine = text.Lines.GetLineFromPosition(awaitedExpression.SpanStart).LineNumber;

            if (awaitLine != expressionLine)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptor,
                        awaitKeyword.GetLocation()));
            }
        }
    }
}
