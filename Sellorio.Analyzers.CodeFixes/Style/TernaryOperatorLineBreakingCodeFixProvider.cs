using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorLineBreakingCodeFixProvider)), Shared]
    public class TernaryOperatorLineBreakingCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Fix ternary operator line breaks";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0002;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var conditionalExpression = FindConditionalExpression(root, diagnostic.Location.SourceSpan);
            if (!TryCreateTextChanges(conditionalExpression, text, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => FixLineBreaksAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static ConditionalExpressionSyntax FindConditionalExpression(SyntaxNode root, TextSpan span) =>
            root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<ConditionalExpressionSyntax>();

        private static async Task<Document> FixLineBreaksAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var conditionalExpression = FindConditionalExpression(root, diagnosticSpan);
            if (!TryCreateTextChanges(conditionalExpression, text, out var changes))
            {
                return document;
            }

            return document.WithText(text.WithChanges(changes));
        }

        private static bool TryCreateTextChanges(
            ConditionalExpressionSyntax conditionalExpression,
            SourceText text,
            out IReadOnlyList<TextChange> changes)
        {
            changes = null;
            if (conditionalExpression == null)
            {
                return false;
            }

            var questionToken = conditionalExpression.QuestionToken;
            var colonToken = conditionalExpression.ColonToken;
            if (questionToken == default || colonToken == default)
            {
                return false;
            }

            if (!HasOnlySupportedTrivia(
                    conditionalExpression.Condition.GetLastToken().TrailingTrivia
                        .Concat(questionToken.LeadingTrivia)
                        .Concat(questionToken.TrailingTrivia)
                        .Concat(conditionalExpression.WhenTrue.GetFirstToken(includeZeroWidth: true).LeadingTrivia))
                || !HasOnlySupportedTrivia(
                    conditionalExpression.WhenTrue.GetLastToken().TrailingTrivia
                        .Concat(colonToken.LeadingTrivia)
                        .Concat(colonToken.TrailingTrivia)
                        .Concat(conditionalExpression.WhenFalse.GetFirstToken(includeZeroWidth: true).LeadingTrivia)))
            {
                return false;
            }

            var startLine = text.Lines.GetLineFromPosition(conditionalExpression.SpanStart);
            var endLine = text.Lines.GetLineFromPosition(conditionalExpression.Span.End);
            if (startLine.LineNumber == endLine.LineNumber)
            {
                return false;
            }

            var lineBreakText = GetLineBreakText(text, startLine.LineNumber);
            var indentation = GetLeadingWhitespace(startLine.ToString()) + "    ";
            changes = new[]
            {
                new TextChange(
                    TextSpan.FromBounds(conditionalExpression.Condition.Span.End, conditionalExpression.WhenTrue.SpanStart),
                    lineBreakText + indentation + "? "),
                new TextChange(
                    TextSpan.FromBounds(conditionalExpression.WhenTrue.Span.End, conditionalExpression.WhenFalse.SpanStart),
                    lineBreakText + indentation + ": "),
            };

            return true;
        }

        private static bool HasOnlySupportedTrivia(IEnumerable<SyntaxTrivia> trivia) =>
            trivia.All(item => item.IsKind(SyntaxKind.WhitespaceTrivia) || item.IsKind(SyntaxKind.EndOfLineTrivia));

        private static string GetLeadingWhitespace(string text)
        {
            var length = 0;
            while (length < text.Length && (text[length] == ' ' || text[length] == '\t'))
            {
                length++;
            }

            return text.Substring(0, length);
        }

        private static string GetLineBreakText(SourceText text, int lineNumber)
        {
            var line = text.Lines[lineNumber];
            return line.SpanIncludingLineBreak.Length > line.Span.Length
                ? text.ToString(TextSpan.FromBounds(line.Span.End, line.SpanIncludingLineBreak.End))
                : Environment.NewLine;
        }
    }
}
