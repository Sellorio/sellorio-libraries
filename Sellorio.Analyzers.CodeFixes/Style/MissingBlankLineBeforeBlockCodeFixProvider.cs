using System;
using System.Composition;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingBlankLineBeforeBlockCodeFixProvider)), Shared]
    public class MissingBlankLineBeforeBlockCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Add blank line before block";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0015;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var blockToken = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (!TryCreateTextChange(text, blockToken, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => AddBlankLineAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> AddBlankLineAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var blockToken = root.FindToken(diagnosticSpan.Start);
            if (!TryCreateTextChange(text, blockToken, out var textChange))
            {
                return document;
            }

            return document.WithText(text.WithChanges(textChange));
        }

        private static bool TryCreateTextChange(SourceText text, SyntaxToken blockToken, out TextChange textChange)
        {
            textChange = default;
            if (blockToken == default)
            {
                return false;
            }

            var insertionLineNumber = text.Lines.GetLineFromPosition(blockToken.Span.Start).LineNumber;
            while (insertionLineNumber > 0)
            {
                var previousLineText = text.Lines[insertionLineNumber - 1].ToString().Trim();
                if (!previousLineText.StartsWith("//"))
                {
                    break;
                }

                insertionLineNumber--;
            }

            if (insertionLineNumber <= 0)
            {
                return false;
            }

            var precedingLineText = text.Lines[insertionLineNumber - 1].ToString().Trim();
            if (string.IsNullOrEmpty(precedingLineText) || precedingLineText.EndsWith("{"))
            {
                return false;
            }

            var insertionPosition = text.Lines[insertionLineNumber].Span.Start;
            textChange = new TextChange(new TextSpan(insertionPosition, 0), GetLineBreakText(text, insertionLineNumber - 1));
            return true;
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
