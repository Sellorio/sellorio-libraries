using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CloseParenthesisMustNotBeFirstOnLineAnalyzer : AnalyzerBase<CloseParenthesisMustNotBeFirstOnLineAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0033;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);
            var text = context.Tree.GetText(context.CancellationToken);

            foreach (var token in root.DescendantTokens())
            {
                if (!token.IsKind(SyntaxKind.CloseParenToken))
                {
                    continue;
                }

                var prevToken = token.GetPreviousToken();

                if (prevToken.IsKind(SyntaxKind.None))
                {
                    continue;
                }

                var closeParenLine = text.Lines.GetLineFromPosition(token.SpanStart).LineNumber;
                var prevTokenLine = text.Lines.GetLineFromPosition(prevToken.Span.End).LineNumber;

                if (closeParenLine > prevTokenLine)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptor, token.GetLocation()));
                }
            }
        }
    }
}
