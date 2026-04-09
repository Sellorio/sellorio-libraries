using System;
using System.Linq.Expressions;
using Sellorio.Analyzers.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TernaryOperatorLineBreakingAnalyzer : AnalyzerBase<TernaryOperatorLineBreakingAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0002;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
        }

        private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
        {
            var syntax = (ConditionalExpressionSyntax)context.Node;
            var text = syntax.SyntaxTree.GetText(context.CancellationToken);
            var questionToken = syntax.QuestionToken;
            var colonToken = syntax.ColonToken;

            var startLine = text.Lines.GetLineFromPosition(syntax.SpanStart);
            var endLine = text.Lines.GetLineFromPosition(syntax.Span.End);

            if (startLine.LineNumber == endLine.LineNumber)
            {
                return;
            }

            var startIndentationWidth = startLine.GetIndentationWidth();

            var questionLine = text.Lines.GetLineFromPosition(questionToken.SpanStart);
            var colonLine = text.Lines.GetLineFromPosition(colonToken.SpanStart);

            if (questionLine.LineNumber == startLine.LineNumber ||
                questionLine.Start != questionToken.LeadingTrivia.Span.Start ||
                questionLine.GetIndentationWidth() != startIndentationWidth + 4 ||
                colonLine.LineNumber == questionLine.LineNumber ||
                colonLine.Start != colonToken.LeadingTrivia.Span.Start ||
                colonLine.GetIndentationWidth() != startIndentationWidth + 4)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, syntax.GetLocation()));
            }
        }
    }
}
