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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitMustNotBeLastOnLineCodeFixProvider)), Shared]
    public class AwaitMustNotBeLastOnLineCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Move awaited expression to same line";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0030;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var awaitExpression = FindAwaitExpression(root, diagnostic.Location.SourceSpan);
            if (!TryCreateTextChanges(awaitExpression, text, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => MoveAwaitedExpressionAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static AwaitExpressionSyntax FindAwaitExpression(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?
                .AncestorsAndSelf()
                .OfType<AwaitExpressionSyntax>()
                .FirstOrDefault(awaitExpression => awaitExpression.AwaitKeyword.Span.IntersectsWith(span));
        }

        private static async Task<Document> MoveAwaitedExpressionAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var awaitExpression = FindAwaitExpression(root, diagnosticSpan);
            if (!TryCreateTextChanges(awaitExpression, text, out var changes))
            {
                return document;
            }

            return document.WithText(text.WithChanges(changes));
        }

        private static bool TryCreateTextChanges(
            AwaitExpressionSyntax awaitExpression,
            SourceText text,
            out IReadOnlyList<TextChange> changes)
        {
            changes = null;
            if (awaitExpression == null)
            {
                return false;
            }

            var firstToken = awaitExpression.Expression.GetFirstToken(includeZeroWidth: true);
            if (firstToken == default)
            {
                return false;
            }

            var interTokenTrivia = awaitExpression.AwaitKeyword.TrailingTrivia.Concat(firstToken.LeadingTrivia).ToList();
            if (interTokenTrivia.Any(trivia => !IsSupportedTrivia(trivia)))
            {
                return false;
            }

            var textChanges = new List<TextChange>
            {
                new TextChange(
                    TextSpan.FromBounds(awaitExpression.AwaitKeyword.Span.End, firstToken.SpanStart),
                    CreateInterTokenText(interTokenTrivia))
            };

            var awaitLine = text.Lines.GetLineFromPosition(awaitExpression.AwaitKeyword.SpanStart);
            var expressionStartLine = text.Lines.GetLineFromPosition(awaitExpression.Expression.SpanStart);
            var indentAdjustment = GetIndentAdjustment(awaitLine.ToString(), expressionStartLine.ToString());
            if (!string.IsNullOrEmpty(indentAdjustment))
            {
                var expressionEndLineNumber = text.Lines.GetLineFromPosition(awaitExpression.Expression.Span.End).LineNumber;
                for (var lineNumber = expressionStartLine.LineNumber + 1; lineNumber <= expressionEndLineNumber; lineNumber++)
                {
                    var line = text.Lines[lineNumber];
                    var updatedLineText = RemoveIndentAdjustment(line.ToString(), indentAdjustment);
                    if (updatedLineText != line.ToString())
                    {
                        textChanges.Add(new TextChange(line.Span, updatedLineText));
                    }
                }
            }

            changes = textChanges;

            return true;
        }

        private static string CreateInterTokenText(IReadOnlyList<SyntaxTrivia> interTokenTrivia)
        {
            var preservedComments = interTokenTrivia
                .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .Select(trivia => trivia.ToFullString())
                .ToList();

            return preservedComments.Count == 0
                ? " "
                : " " + string.Join(" ", preservedComments) + " ";
        }

        private static string GetIndentAdjustment(string awaitLineText, string expressionLineText)
        {
            var awaitIndent = GetLeadingWhitespace(awaitLineText);
            var expressionIndent = GetLeadingWhitespace(expressionLineText);
            if (expressionIndent.Length <= awaitIndent.Length)
            {
                return string.Empty;
            }

            return expressionIndent.StartsWith(awaitIndent, StringComparison.Ordinal)
                ? expressionIndent.Substring(awaitIndent.Length)
                : expressionIndent.Substring(0, expressionIndent.Length - awaitIndent.Length);
        }

        private static bool IsSupportedTrivia(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.WhitespaceTrivia)
                || trivia.IsKind(SyntaxKind.EndOfLineTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);

        private static string GetLeadingWhitespace(string text)
        {
            var length = 0;
            while (length < text.Length && (text[length] == ' ' || text[length] == '\t'))
            {
                length++;
            }

            return text.Substring(0, length);
        }

        private static string RemoveIndentAdjustment(string lineText, string indentAdjustment)
        {
            if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(indentAdjustment))
            {
                return lineText;
            }

            if (lineText.StartsWith(indentAdjustment, StringComparison.Ordinal))
            {
                return lineText.Substring(indentAdjustment.Length);
            }

            var removableLength = Math.Min(indentAdjustment.Length, GetLeadingWhitespace(lineText).Length);
            if (removableLength == 0)
            {
                return lineText;
            }

            return lineText.Substring(removableLength);
        }
    }
}
