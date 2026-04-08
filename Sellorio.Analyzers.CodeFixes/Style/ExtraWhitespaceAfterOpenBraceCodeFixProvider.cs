using System;
using System.Composition;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtraWhitespaceAfterOpenBraceCodeFixProvider)), Shared]
    public class ExtraWhitespaceAfterOpenBraceCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Remove extra blank lines after open brace";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0012;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var openBraceToken = FindOpenBraceToken(root, diagnostic.Location.SourceSpan);
            if (!TryCreateTextChange(text, openBraceToken, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => RemoveBlankLinesAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static SyntaxToken FindOpenBraceToken(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.IsKind(SyntaxKind.OpenBraceToken) && token.Span.IntersectsWith(span)
                ? token
                : default;
        }

        private static async Task<Document> RemoveBlankLinesAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var openBraceToken = FindOpenBraceToken(root, diagnosticSpan);
            if (!TryCreateTextChange(text, openBraceToken, out var textChange))
            {
                return document;
            }

            return document.WithText(text.WithChanges(textChange));
        }

        private static bool TryCreateTextChange(SourceText text, SyntaxToken openBraceToken, out TextChange textChange)
        {
            textChange = default;
            if (openBraceToken == default || !openBraceToken.IsKind(SyntaxKind.OpenBraceToken))
            {
                return false;
            }

            var braceLine = text.Lines.GetLineFromPosition(openBraceToken.Span.End);
            var firstBlankLineNumber = braceLine.LineNumber + 1;
            if (firstBlankLineNumber >= text.Lines.Count)
            {
                return false;
            }

            var lineNumber = firstBlankLineNumber;
            while (lineNumber < text.Lines.Count)
            {
                var lineText = text.Lines[lineNumber].ToString();
                if (!string.IsNullOrWhiteSpace(lineText))
                {
                    break;
                }

                lineNumber++;
            }

            if (lineNumber == firstBlankLineNumber)
            {
                return false;
            }

            var start = text.Lines[firstBlankLineNumber].Span.Start;
            var end = lineNumber < text.Lines.Count
                ? text.Lines[lineNumber].Span.Start
                : text.Lines[lineNumber - 1].SpanIncludingLineBreak.End;

            textChange = new TextChange(TextSpan.FromBounds(start, end), string.Empty);
            return true;
        }
    }
}
