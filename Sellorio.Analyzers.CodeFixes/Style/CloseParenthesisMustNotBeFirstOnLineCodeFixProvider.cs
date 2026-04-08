using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CloseParenthesisMustNotBeFirstOnLineCodeFixProvider)), Shared]
    public class CloseParenthesisMustNotBeFirstOnLineCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Move close parenthesis to previous line";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0033;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics[0];
            var closeParenToken = FindCloseParenToken(root, diagnostic.Location.SourceSpan);
            if (!TryCreateTextChanges(root, text, closeParenToken, out _, out _))
                return;

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => MoveCloseParenthesisAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static SyntaxToken FindCloseParenToken(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.IsKind(SyntaxKind.CloseParenToken) && token.Span.IntersectsWith(span)
                ? token
                : default;
        }

        private static async Task<Document> MoveCloseParenthesisAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var closeParenToken = FindCloseParenToken(root, diagnosticSpan);
            if (!TryCreateTextChanges(root, text, closeParenToken, out var previousLineChange, out var closeParenLineChange))
                return document;

            return document.WithText(text.WithChanges(previousLineChange, closeParenLineChange));
        }

        private static bool TryCreateTextChanges(
            SyntaxNode root,
            SourceText text,
            SyntaxToken closeParenToken,
            out TextChange previousLineChange,
            out TextChange closeParenLineChange)
        {
            previousLineChange = default;
            closeParenLineChange = default;
            if (closeParenToken == default || !closeParenToken.IsKind(SyntaxKind.CloseParenToken))
                return false;

            var closeParenLine = text.Lines.GetLineFromPosition(closeParenToken.SpanStart);
            if (closeParenLine.LineNumber == 0)
                return false;

            var closeParenLineText = closeParenLine.ToString();
            var firstNonWhitespaceIndex = GetFirstNonWhitespaceIndex(closeParenLineText);
            if (firstNonWhitespaceIndex < 0 || closeParenLineText[firstNonWhitespaceIndex] != ')')
                return false;

            var previousLine = text.Lines[closeParenLine.LineNumber - 1];
            var previousLineText = previousLine.ToString();

            var previousComment = FindSingleLineComment(root, previousLine);
            var currentComment = FindSingleLineComment(root, closeParenLine);

            var previousCommentStart = previousComment == default
                ? previousLineText.Length
                : previousComment.SpanStart - previousLine.Span.Start;
            var currentCommentStart = currentComment == default
                ? closeParenLineText.Length
                : currentComment.SpanStart - closeParenLine.Span.Start;

            var updatedPreviousLine = previousLineText.Substring(0, previousCommentStart).TrimEnd()
                + closeParenLineText.Substring(firstNonWhitespaceIndex, currentCommentStart - firstNonWhitespaceIndex).TrimEnd();

            var mergedComment = MergeComments(
                previousComment == default ? null : previousComment.ToString(),
                currentComment == default ? null : currentComment.ToString());
            if (!string.IsNullOrEmpty(mergedComment))
            {
                updatedPreviousLine += " " + mergedComment;
            }

            previousLineChange = new TextChange(previousLine.Span, updatedPreviousLine);
            closeParenLineChange = new TextChange(closeParenLine.SpanIncludingLineBreak, string.Empty);
            return true;
        }

        private static SyntaxTrivia FindSingleLineComment(SyntaxNode root, TextLine line) =>
            root.DescendantTrivia(descendIntoTrivia: true)
                .FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && line.Span.IntersectsWith(trivia.Span));

        private static int GetFirstNonWhitespaceIndex(string lineText)
        {
            for (var index = 0; index < lineText.Length; index++)
            {
                if (lineText[index] != ' ' && lineText[index] != '\t')
                    return index;
            }

            return -1;
        }

        private static string MergeComments(string previousComment, string currentComment)
        {
            if (string.IsNullOrEmpty(previousComment))
                return currentComment;

            if (string.IsNullOrEmpty(currentComment))
                return previousComment;

            var previousCommentBody = GetCommentBody(previousComment);
            var currentCommentBody = GetCommentBody(currentComment);
            if (string.IsNullOrEmpty(previousCommentBody))
                return string.IsNullOrEmpty(currentCommentBody) ? "//" : "// " + currentCommentBody;

            if (string.IsNullOrEmpty(currentCommentBody))
                return "// " + previousCommentBody;

            return "// " + previousCommentBody + ". " + currentCommentBody;
        }

        private static string GetCommentBody(string comment)
        {
            var commentBody = comment.StartsWith("//", StringComparison.Ordinal)
                ? comment.Substring(2)
                : comment;

            return commentBody.Trim();
        }
    }
}
