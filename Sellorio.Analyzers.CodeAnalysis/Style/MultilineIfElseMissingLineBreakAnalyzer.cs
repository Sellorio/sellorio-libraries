using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MultilineIfElseMissingLineBreakAnalyzer : AnalyzerBase<MultilineIfElseMissingLineBreakAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0014;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        }

        private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            // Ensure this is an "else if"
            if (!(ifStatement.Parent is ElseClauseSyntax elseClause) ||
                elseClause.Statement != ifStatement)
            {
                return;
            }

            var condition = ifStatement.Condition;

            var syntaxTree = context.Node.SyntaxTree;
            var text = syntaxTree.GetText(context.CancellationToken);

            var conditionSpan = condition.Span;
            var ifKeyword = ifStatement.IfKeyword;

            var conditionStartLine = text.Lines.GetLineFromPosition(conditionSpan.Start);
            var conditionEndLine = text.Lines.GetLineFromPosition(conditionSpan.End);

            // Only care about multi-line conditions
            if (conditionStartLine.LineNumber == conditionEndLine.LineNumber)
            {
                return;
            }

            var ifLine = text.Lines.GetLineFromPosition(ifKeyword.SpanStart);

            // If condition starts on same line as "if", flag it
            if (conditionStartLine.LineNumber == ifLine.LineNumber)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptor, condition.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
