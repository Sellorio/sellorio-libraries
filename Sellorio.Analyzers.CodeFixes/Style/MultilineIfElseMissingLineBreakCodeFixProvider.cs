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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MultilineIfElseMissingLineBreakCodeFixProvider)), Shared]
    public class MultilineIfElseMissingLineBreakCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Move if condition to next line";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0014;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            if (!TryFindTarget(root, diagnostic.Location.SourceSpan, out var ifStatement)
                || !TryCreateTextChanges(text, ifStatement, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => MoveConditionToNextLineAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> MoveConditionToNextLineAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            if (!TryFindTarget(root, diagnosticSpan, out var ifStatement)
                || !TryCreateTextChanges(text, ifStatement, out var changes))
            {
                return document;
            }

            return document.WithText(text.WithChanges(changes));
        }

        private static bool TryFindTarget(SyntaxNode root, TextSpan span, out IfStatementSyntax ifStatement)
        {
            ifStatement = root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<IfStatementSyntax>();
            return ifStatement != null
                && ifStatement.Condition != null
                && ifStatement.Condition.SpanStart <= span.Start
                && ifStatement.Condition.Span.End >= span.End;
        }

        private static bool TryCreateTextChanges(SourceText text, IfStatementSyntax ifStatement, out IReadOnlyList<TextChange> changes)
        {
            changes = null;
            if (ifStatement?.Condition == null)
            {
                return false;
            }

            var openParenToken = ifStatement.OpenParenToken;
            var condition = ifStatement.Condition;
            var firstToken = condition.GetFirstToken(includeZeroWidth: true);
            if (openParenToken == default || firstToken == default)
            {
                return false;
            }

            var interTokenTrivia = openParenToken.TrailingTrivia.Concat(firstToken.LeadingTrivia).ToList();
            if (interTokenTrivia.Any(trivia => !IsSupportedTrivia(trivia)))
            {
                return false;
            }

            var ifLine = text.Lines.GetLineFromPosition(ifStatement.IfKeyword.SpanStart);
            var conditionStartLineNumber = text.Lines.GetLineFromPosition(condition.SpanStart).LineNumber;
            var conditionEndLineNumber = text.Lines.GetLineFromPosition(condition.Span.End).LineNumber;
            if (conditionStartLineNumber != ifLine.LineNumber || conditionStartLineNumber == conditionEndLineNumber)
            {
                return false;
            }

            var ifIndent = GetLeadingWhitespace(ifLine.ToString());
            var indentAdjustment = GetIndentAdjustment(text, ifIndent, conditionStartLineNumber + 1, conditionEndLineNumber);
            var updatedConditionIndent = ifIndent + indentAdjustment;

            var textChanges = new List<TextChange>
            {
                new TextChange(
                    TextSpan.FromBounds(openParenToken.Span.End, firstToken.SpanStart),
                    CreateInterTokenText(GetLineBreakText(text, ifLine.LineNumber), updatedConditionIndent, interTokenTrivia))
            };

            if (!string.IsNullOrEmpty(indentAdjustment))
            {
                for (var lineNumber = conditionStartLineNumber + 1; lineNumber <= conditionEndLineNumber; lineNumber++)
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

        private static bool IsSupportedTrivia(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.WhitespaceTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);
    }
}
