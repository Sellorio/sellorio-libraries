using System;
using System.Linq;
using System.Linq.Expressions;
using Sellorio.Analyzers.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TernaryOperatorLineBreakingAnalyzer : AnalyzerBase<TernaryOperatorLineBreakingAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0002;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterOperationAction(
                operationContext =>
                {
                    var ternaryOperation = (Microsoft.CodeAnalysis.Operations.IConditionalOperation)operationContext.Operation;
                    var syntax = ternaryOperation.Syntax;
                    var text = syntax.SyntaxTree.GetText(operationContext.CancellationToken);
                    var questionToken = syntax.DescendantTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.QuestionToken));
                    var colonToken = syntax.DescendantTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.ColonToken));

                    // Is ternary
                    if (questionToken != default && colonToken != default)
                    {
                        var startLine = text.Lines.GetLineFromPosition(syntax.SpanStart);
                        var endLine = text.Lines.GetLineFromPosition(syntax.Span.End);

                        // Is across multiple lines
                        if (startLine != endLine)
                        {
                            var startIndentationWidth = startLine.GetIndentationWidth();

                            var questionLine = text.Lines.GetLineFromPosition(questionToken.SpanStart);
                            var colonLine = text.Lines.GetLineFromPosition(colonToken.SpanStart);

                            if (questionLine == startLine ||
                                questionLine.Start != questionToken.LeadingTrivia.Span.Start ||
                                questionLine.GetIndentationWidth() != startIndentationWidth + 4 ||
                                colonLine == questionLine ||
                                colonLine.Start != colonToken.LeadingTrivia.Span.Start ||
                                colonLine.GetIndentationWidth() != startIndentationWidth + 4)
                            {
                                operationContext.ReportDiagnostic(
                                    Diagnostic.Create(DiagnosticDescriptor, Location.Create(syntax.SyntaxTree, syntax.Span)));

                                return;
                            }
                        }
                    }
                },
                OperationKind.Conditional);
        }
    }
}
