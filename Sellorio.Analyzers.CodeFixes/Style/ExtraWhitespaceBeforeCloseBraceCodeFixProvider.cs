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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtraWhitespaceBeforeCloseBraceCodeFixProvider)), Shared]
    public class ExtraWhitespaceBeforeCloseBraceCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Remove extra blank lines before close brace";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0013;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var closeBraceToken = FindCloseBraceToken(root, diagnostic.Location.SourceSpan);
            if (!TryCreateTextChange(text, closeBraceToken, out _))
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

        private static SyntaxToken FindCloseBraceToken(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.IsKind(SyntaxKind.CloseBraceToken) && token.Span.IntersectsWith(span)
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

            var closeBraceToken = FindCloseBraceToken(root, diagnosticSpan);
            if (!TryCreateTextChange(text, closeBraceToken, out var textChange))
            {
                return document;
            }

            return document.WithText(text.WithChanges(textChange));
        }

        private static bool TryCreateTextChange(SourceText text, SyntaxToken closeBraceToken, out TextChange textChange)
        {
            textChange = default;
            if (closeBraceToken == default || !closeBraceToken.IsKind(SyntaxKind.CloseBraceToken))
            {
                return false;
            }

            var braceLine = text.Lines.GetLineFromPosition(closeBraceToken.Span.End);
            var firstBlankLineNumber = braceLine.LineNumber - 1;
            if (firstBlankLineNumber < 0 || !string.IsNullOrWhiteSpace(text.Lines[firstBlankLineNumber].ToString()))
            {
                return false;
            }

            var lineNumber = firstBlankLineNumber;
            while (lineNumber >= 0)
            {
                var lineText = text.Lines[lineNumber].ToString();
                if (!string.IsNullOrWhiteSpace(lineText))
                {
                    break;
                }

                lineNumber--;
            }

            var start = lineNumber >= 0
                ? text.Lines[lineNumber].SpanIncludingLineBreak.End
                : 0;
            var end = text.Lines[braceLine.LineNumber].Span.Start;

            textChange = new TextChange(TextSpan.FromBounds(start, end), string.Empty);
            return true;
        }
    }
}
