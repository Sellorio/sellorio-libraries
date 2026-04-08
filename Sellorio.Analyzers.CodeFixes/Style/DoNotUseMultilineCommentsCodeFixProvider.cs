using System;
using System.Collections.Generic;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DoNotUseMultilineCommentsCodeFixProvider)), Shared]
    public class DoNotUseMultilineCommentsCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Convert multiline comment to single-line comments";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0025;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics[0];
            var commentTrivia = FindCommentTrivia(root, diagnostic.Location.SourceSpan);
            if (!TryCreateReplacement(text, commentTrivia, out _))
                return;

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => ConvertCommentAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static SyntaxTrivia FindCommentTrivia(SyntaxNode root, TextSpan span) =>
            root.DescendantTrivia(descendIntoTrivia: true)
                .FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) && trivia.Span.IntersectsWith(span));

        private static async Task<Document> ConvertCommentAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var commentTrivia = FindCommentTrivia(root, diagnosticSpan);
            if (!TryCreateReplacement(text, commentTrivia, out var replacement))
                return document;

            return document.WithText(text.WithChanges(new TextChange(commentTrivia.Span, replacement)));
        }

        private static bool TryCreateReplacement(SourceText text, SyntaxTrivia commentTrivia, out string replacement)
        {
            replacement = string.Empty;
            if (commentTrivia == default || !commentTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                return false;

            var startLine = text.Lines.GetLineFromPosition(commentTrivia.Span.Start);
            var endLine = text.Lines.GetLineFromPosition(Math.Max(commentTrivia.Span.Start, commentTrivia.Span.End - 1));
            var hasTrailingText = HasTrailingText(text, commentTrivia.Span.End, endLine.Span.End);
            if (startLine.LineNumber == endLine.LineNumber && hasTrailingText)
                return false;

            if (startLine.LineNumber == endLine.LineNumber)
            {
                var singleLineBody = text.ToString(TextSpan.FromBounds(commentTrivia.Span.Start + 2, commentTrivia.Span.End - 2));
                replacement = FormatSingleLineComment(singleLineBody, preserveIndentation: false, skipEmpty: false) ?? string.Empty;
                return replacement.Length > 0;
            }

            var lines = new List<string>();
            var lineBreak = GetLineBreakText(text, startLine);
            var startLineBody = text.ToString(TextSpan.FromBounds(commentTrivia.Span.Start + 2, startLine.Span.End));
            AddSingleLineComment(lines, startLineBody, preserveIndentation: false);

            for (var lineNumber = startLine.LineNumber + 1; lineNumber < endLine.LineNumber; lineNumber++)
            {
                var line = text.Lines[lineNumber];
                AddSingleLineComment(lines, line.ToString(), preserveIndentation: true);
            }

            var endLineBody = text.ToString(TextSpan.FromBounds(endLine.Span.Start, commentTrivia.Span.End - 2));
            if (hasTrailingText)
            {
                lines.Add(FormatRemainingMultilineComment(endLineBody));
            }
            else
            {
                AddSingleLineComment(lines, endLineBody, preserveIndentation: true);
            }

            if (lines.Count == 0)
                return false;

            lines[0] = lines[0].TrimStart(' ', '\t');
            replacement = string.Join(lineBreak, lines);
            return replacement.Length > 0;
        }

        private static bool HasTrailingText(SourceText text, int commentEnd, int lineEnd)
        {
            for (var position = commentEnd; position < lineEnd; position++)
            {
                if (!char.IsWhiteSpace(text[position]))
                    return true;
            }

            return false;
        }

        private static string GetLineBreakText(SourceText text, TextLine line) =>
            text.ToString(TextSpan.FromBounds(line.Span.End, line.SpanIncludingLineBreak.End));

        private static void AddSingleLineComment(ICollection<string> lines, string commentBody, bool preserveIndentation)
        {
            var formattedComment = FormatSingleLineComment(commentBody, preserveIndentation, skipEmpty: true);
            if (formattedComment == null)
                return;

            lines.Add(formattedComment);
        }

        private static string FormatSingleLineComment(string commentBody, bool preserveIndentation, bool skipEmpty)
        {
            var (indentation, trimmedCommentBody) = NormalizeCommentBody(commentBody, preserveIndentation);
            if (string.IsNullOrEmpty(trimmedCommentBody))
                return skipEmpty ? null : indentation + "//";

            return indentation + "// " + trimmedCommentBody;
        }

        private static string FormatRemainingMultilineComment(string commentBody)
        {
            var (indentation, trimmedCommentBody) = NormalizeCommentBody(commentBody, preserveIndentation: true);
            return string.IsNullOrEmpty(trimmedCommentBody)
                ? string.Empty
                : indentation + "/* " + trimmedCommentBody + " */";
        }

        private static (string Indentation, string CommentBody) NormalizeCommentBody(string commentBody, bool preserveIndentation)
        {
            var indentationLength = 0;
            if (preserveIndentation)
            {
                while (indentationLength < commentBody.Length && (commentBody[indentationLength] == ' ' || commentBody[indentationLength] == '\t'))
                {
                    indentationLength++;
                }
            }

            var indentation = commentBody.Substring(0, indentationLength);
            var normalizedCommentBody = commentBody.Substring(indentationLength);
            if (preserveIndentation && normalizedCommentBody.StartsWith("*", StringComparison.Ordinal))
            {
                normalizedCommentBody = normalizedCommentBody.Substring(1);
                if (normalizedCommentBody.StartsWith(" ", StringComparison.Ordinal))
                {
                    normalizedCommentBody = normalizedCommentBody.Substring(1);
                }

                if (indentation.Length > 0)
                {
                    indentation = indentation.Substring(0, indentation.Length - 1);
                }
            }

            return (indentation, normalizedCommentBody.Trim());
        }
    }
}
