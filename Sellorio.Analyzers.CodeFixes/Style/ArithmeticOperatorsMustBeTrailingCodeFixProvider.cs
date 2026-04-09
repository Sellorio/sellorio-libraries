using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArithmeticOperatorsMustBeTrailingCodeFixProvider)), Shared]
    public class ArithmeticOperatorsMustBeTrailingCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Move operator to previous line";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0009;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var binaryExpression = FindBinaryExpression(root, diagnostic.Location.SourceSpan);

            if (!TryCreateTextChanges(binaryExpression, text, out _, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => MoveOperatorAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static BinaryExpressionSyntax FindBinaryExpression(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);

            return token.Parent?
                .AncestorsAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault(binary => binary.OperatorToken.Span.IntersectsWith(span));
        }

        private static async Task<Document> MoveOperatorAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return document;
            }

            var binaryExpression = FindBinaryExpression(root, diagnosticSpan);

            return
                !TryCreateTextChanges(binaryExpression, text, out var leftLineChange, out var operatorLineChange)
                    ? document
                    : document.WithText(text.WithChanges(leftLineChange, operatorLineChange));
        }

        private static bool TryCreateTextChanges(BinaryExpressionSyntax binaryExpression, SourceText text, out TextChange leftLineChange, out TextChange operatorLineChange)
        {
            leftLineChange = default;
            operatorLineChange = default;

            if (binaryExpression == null)
            {
                return false;
            }

            var leftLine = text.Lines.GetLineFromPosition(binaryExpression.Left.Span.End);
            var operatorLine = text.Lines.GetLineFromPosition(binaryExpression.OperatorToken.SpanStart);

            if (operatorLine.LineNumber <= leftLine.LineNumber)
            {
                return false;
            }

            var leftLineText = leftLine.ToString();
            var leftEndInLine = binaryExpression.Left.Span.End - leftLine.Span.Start;
            var trailingText = leftLineText.Substring(leftEndInLine);
            var updatedLeftLine = leftLineText.Substring(0, leftEndInLine).TrimEnd() + " " + binaryExpression.OperatorToken.Text;

            if (!string.IsNullOrWhiteSpace(trailingText))
            {
                updatedLeftLine += " " + trailingText.TrimStart();
            }

            leftLineChange = new TextChange(leftLine.Span, updatedLeftLine);

            var operatorLineText = operatorLine.ToString();
            var operatorStartInLine = binaryExpression.OperatorToken.SpanStart - operatorLine.Span.Start;
            var operatorEndInLine = binaryExpression.OperatorToken.Span.End - operatorLine.Span.Start;

            while (
                operatorEndInLine < operatorLineText.Length &&
                (operatorLineText[operatorEndInLine] == ' ' || operatorLineText[operatorEndInLine] == '\t'))
            {
                operatorEndInLine++;
            }

            var updatedOperatorLine = operatorLineText.Remove(operatorStartInLine, operatorEndInLine - operatorStartInLine);

            operatorLineChange = string.IsNullOrWhiteSpace(updatedOperatorLine)
                ? new TextChange(operatorLine.SpanIncludingLineBreak, string.Empty)
                : new TextChange(operatorLine.Span, updatedOperatorLine);

            return true;
        }
    }
}
