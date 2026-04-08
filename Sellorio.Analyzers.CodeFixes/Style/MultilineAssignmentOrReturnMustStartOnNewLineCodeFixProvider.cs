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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MultilineAssignmentOrReturnMustStartOnNewLineCodeFixProvider)), Shared]
    public class MultilineAssignmentOrReturnMustStartOnNewLineCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Move expression to next line";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0021;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            if (!TryFindTarget(root, diagnostic.Location.SourceSpan, out var operatorOrKeyword, out var expression)
                || !TryCreateTextChanges(text, operatorOrKeyword, expression, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => MoveExpressionToNextLineAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> MoveExpressionToNextLineAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            if (!TryFindTarget(root, diagnosticSpan, out var operatorOrKeyword, out var expression)
                || !TryCreateTextChanges(text, operatorOrKeyword, expression, out var changes))
            {
                return document;
            }

            return document.WithText(text.WithChanges(changes));
        }

        private static bool TryFindTarget(SyntaxNode root, TextSpan span, out SyntaxToken operatorOrKeyword, out ExpressionSyntax expression)
        {
            operatorOrKeyword = default;
            expression = null;

            var node = root.FindNode(span, getInnermostNodeForTie: true);
            expression = node as ExpressionSyntax ?? node.FirstAncestorOrSelf<ExpressionSyntax>();
            if (expression == null)
            {
                return false;
            }

            if (expression.Parent is ReturnStatementSyntax returnStatement && returnStatement.Expression == expression)
            {
                operatorOrKeyword = returnStatement.ReturnKeyword;
                return true;
            }

            if (expression.Parent is AssignmentExpressionSyntax assignment && assignment.Right == expression)
            {
                operatorOrKeyword = assignment.OperatorToken;
                return true;
            }

            if (expression.Parent is EqualsValueClauseSyntax initializer && initializer.Value == expression)
            {
                operatorOrKeyword = initializer.EqualsToken;
                return true;
            }

            return false;
        }

        private static bool TryCreateTextChanges(
            SourceText text,
            SyntaxToken operatorOrKeyword,
            ExpressionSyntax expression,
            out IReadOnlyList<TextChange> changes)
        {
            changes = null;
            if (operatorOrKeyword == default || expression == null)
            {
                return false;
            }

            var firstToken = expression.GetFirstToken(includeZeroWidth: true);
            if (firstToken == default)
            {
                return false;
            }

            var interTokenTrivia = operatorOrKeyword.TrailingTrivia.Concat(firstToken.LeadingTrivia).ToList();
            if (interTokenTrivia.Any(trivia => !IsSupportedTrivia(trivia)))
            {
                return false;
            }

            var operatorLine = text.Lines.GetLineFromPosition(operatorOrKeyword.SpanStart);
            var expressionStartLineNumber = text.Lines.GetLineFromPosition(expression.SpanStart).LineNumber;
            var expressionEndLineNumber = text.Lines.GetLineFromPosition(expression.Span.End).LineNumber;
            if (!IsRelevantPartMultiLine(text, expression))
            {
                return false;
            }

            var operatorIndent = GetLeadingWhitespace(operatorLine.ToString());
            var indentAdjustment = GetIndentAdjustment(text, operatorIndent, expressionStartLineNumber + 1, expressionEndLineNumber);
            var updatedExpressionIndent = operatorIndent + indentAdjustment;

            var textChanges = new List<TextChange>
            {
                new TextChange(
                    TextSpan.FromBounds(operatorOrKeyword.Span.End, firstToken.SpanStart),
                    CreateInterTokenText(GetLineBreakText(text, operatorLine.LineNumber), updatedExpressionIndent, interTokenTrivia))
            };

            if (!string.IsNullOrEmpty(indentAdjustment))
            {
                for (var lineNumber = expressionStartLineNumber + 1; lineNumber <= expressionEndLineNumber; lineNumber++)
                {
                    var line = text.Lines[lineNumber];
                    if (string.IsNullOrWhiteSpace(line.ToString()))
                    {
                        continue;
                    }

                    textChanges.Add(new TextChange(new TextSpan(line.Span.Start, 0), indentAdjustment));
                }
            }

            changes = textChanges;
            return true;
        }

        private static string CreateInterTokenText(string lineBreakText, string indentation, IReadOnlyList<SyntaxTrivia> interTokenTrivia)
        {
            var preservedComments = interTokenTrivia
                .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .Select(trivia => trivia.ToFullString())
                .ToList();

            return preservedComments.Count == 0
                ? lineBreakText + indentation
                : lineBreakText + indentation + string.Join(" ", preservedComments) + " ";
        }

        private static string GetIndentAdjustment(SourceText text, string baseIndent, int startLineNumber, int endLineNumber)
        {
            string bestCandidate = null;

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                var lineText = text.Lines[lineNumber].ToString();
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    continue;
                }

                var lineIndent = GetLeadingWhitespace(lineText);
                if (lineIndent.Length <= baseIndent.Length)
                {
                    continue;
                }

                var candidate = lineIndent.StartsWith(baseIndent, StringComparison.Ordinal)
                    ? lineIndent.Substring(baseIndent.Length)
                    : lineIndent.Substring(0, lineIndent.Length - baseIndent.Length);

                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                if (bestCandidate == null || candidate.Length < bestCandidate.Length)
                {
                    bestCandidate = candidate;
                }
            }

            return bestCandidate ?? "    ";
        }

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

        private static bool IsRelevantPartMultiLine(SourceText text, ExpressionSyntax expression)
        {
            if (expression is SwitchExpressionSyntax switchExpression)
            {
                var governingExpressionStartLine = text.Lines.GetLineFromPosition(switchExpression.GoverningExpression.SpanStart).LineNumber;
                var governingExpressionEndLine = text.Lines.GetLineFromPosition(switchExpression.GoverningExpression.Span.End).LineNumber;
                return governingExpressionStartLine != governingExpressionEndLine;
            }

            var expressionStartLine = text.Lines.GetLineFromPosition(expression.SpanStart).LineNumber;
            var expressionEndLine = text.Lines.GetLineFromPosition(expression.Span.End).LineNumber;
            return expressionStartLine != expressionEndLine;
        }

        private static bool IsSupportedTrivia(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.WhitespaceTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);
    }
}
