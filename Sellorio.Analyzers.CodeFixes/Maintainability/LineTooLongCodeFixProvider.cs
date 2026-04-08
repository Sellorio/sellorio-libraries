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

namespace Sellorio.Analyzers.CodeFixes.Maintainability
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LineTooLongCodeFixProvider)), Shared]
    public class LineTooLongCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Split long line";
        private const int MaxLineLength = 160;
        private const string DefaultIndent = "    ";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0026;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];

            if (root == null || semanticModel == null)
            {
                return;
            }

            if (!TryRewriteLine(root, text, semanticModel, diagnostic.Location.SourceSpan.Start, context.CancellationToken, out var replacement))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => ApplyFixAsync(context.Document, diagnostic.Location.SourceSpan.Start, replacement, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> ApplyFixAsync(Document document, int position, string replacement, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var line = text.Lines.GetLineFromPosition(position);
            var newText = text.WithChanges(new TextChange(line.Span, replacement));
            return document.WithText(newText);
        }

        private static bool TryRewriteLine(
            SyntaxNode root,
            SourceText text,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken,
            out string replacement)
        {
            replacement = null;

            var line = text.Lines.GetLineFromPosition(position);
            var originalLine = line.ToString();
            if (originalLine.Length <= MaxLineLength)
            {
                return false;
            }

            List<string> rewrittenLines;
            if (!TryFormatAssignmentLine(root, line, originalLine, position, semanticModel, cancellationToken, out rewrittenLines)
                && !TryFormatExpressionStatementLine(root, line, originalLine, position, semanticModel, cancellationToken, out rewrittenLines)
                && !TryFormatIfStatementLine(root, line, originalLine, position, out rewrittenLines)
                && !TryFormatReturnStatementLine(root, line, originalLine, position, semanticModel, cancellationToken, out rewrittenLines))
            {
                return false;
            }

            if (rewrittenLines == null
                || rewrittenLines.Count <= 1
                || rewrittenLines.Any(rewrittenLine => rewrittenLine.Length > MaxLineLength))
            {
                return false;
            }

            replacement = string.Join(GetLineBreak(text, line), rewrittenLines);
            return true;
        }

        private static bool TryFormatAssignmentLine(
            SyntaxNode root,
            TextLine line,
            string originalLine,
            int position,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var token = root.FindToken(position);
            var localDeclaration = token.Parent?.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            if (localDeclaration != null)
            {
                var variable = localDeclaration.Declaration.Variables.Count == 1
                    ? localDeclaration.Declaration.Variables[0]
                    : null;
                if (variable?.Initializer == null || !line.Span.IntersectsWith(variable.Initializer.EqualsToken.Span))
                {
                    return false;
                }

                return TryFormatAssignment(
                    originalLine,
                    line,
                    variable.Initializer.EqualsToken,
                    variable.Initializer.Value,
                    semanticModel,
                    cancellationToken,
                    out rewrittenLines);
            }

            var assignment = token.Parent?.AncestorsAndSelf().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
            if (assignment == null || !line.Span.IntersectsWith(assignment.OperatorToken.Span))
            {
                return false;
            }

            return TryFormatAssignment(
                originalLine,
                line,
                assignment.OperatorToken,
                assignment.Right,
                semanticModel,
                cancellationToken,
                out rewrittenLines);
        }

        private static bool TryFormatAssignment(
            string originalLine,
            TextLine line,
            SyntaxToken equalsToken,
            ExpressionSyntax rightSide,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var indentation = GetLeadingWhitespace(originalLine);
            var continuationIndent = indentation + GetIndentUnit(indentation);
            var prefixLength = equalsToken.Span.End - line.Span.Start;
            var suffixStart = rightSide.Span.End - line.Span.Start;

            var prefix = originalLine.Substring(0, prefixLength).TrimEnd();
            var suffix = originalLine.Substring(suffixStart);
            if (!TryFormatExpression(rightSide, semanticModel, continuationIndent, cancellationToken, out var expressionLines))
            {
                return false;
            }

            rewrittenLines = new List<string> { prefix };
            rewrittenLines.AddRange(expressionLines);
            rewrittenLines[rewrittenLines.Count - 1] += suffix;
            return true;
        }

        private static bool TryFormatExpressionStatementLine(
            SyntaxNode root,
            TextLine line,
            string originalLine,
            int position,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var token = root.FindToken(position);
            var statement = token.Parent?.AncestorsAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
            if (statement == null)
            {
                return false;
            }

            if (!TryFormatExpression(statement.Expression, semanticModel, GetLeadingWhitespace(originalLine), cancellationToken, out rewrittenLines)
                || rewrittenLines.Count <= 1)
            {
                return false;
            }

            rewrittenLines[rewrittenLines.Count - 1] += originalLine.Substring(statement.Expression.Span.End - line.Span.Start);
            return true;
        }

        private static bool TryFormatIfStatementLine(
            SyntaxNode root,
            TextLine line,
            string originalLine,
            int position,
            out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var token = root.FindToken(position);
            var ifStatement = token.Parent?.AncestorsAndSelf().OfType<IfStatementSyntax>().FirstOrDefault();
            if (ifStatement == null)
            {
                return false;
            }

            if (!TryGetBinarySegments(ifStatement.Condition, IsBooleanOperator, out var operands, out var operators))
            {
                return false;
            }

            var indentation = GetLeadingWhitespace(originalLine);
            var continuationIndent = indentation + GetIndentUnit(indentation);
            var conditionStart = ifStatement.Condition.Span.Start - line.Span.Start;
            var conditionEnd = ifStatement.Condition.Span.End - line.Span.Start;
            var prefix = originalLine.Substring(0, conditionStart);
            var suffix = originalLine.Substring(conditionEnd);

            rewrittenLines = BuildOperatorSeparatedLines(prefix, continuationIndent, suffix, operands, operators);
            return rewrittenLines.Count > 1;
        }

        private static bool TryFormatReturnStatementLine(
            SyntaxNode root,
            TextLine line,
            string originalLine,
            int position,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var token = root.FindToken(position);
            var returnStatement = token.Parent?.AncestorsAndSelf().OfType<ReturnStatementSyntax>().FirstOrDefault();
            if (returnStatement?.Expression == null)
            {
                return false;
            }

            var indentation = GetLeadingWhitespace(originalLine);
            var continuationIndent = indentation + GetIndentUnit(indentation);
            var prefixLength = returnStatement.Expression.SpanStart - line.Span.Start;
            var suffix = originalLine.Substring(returnStatement.Expression.Span.End - line.Span.Start);
            var prefix = originalLine.Substring(0, prefixLength).TrimEnd();

            if (!TryFormatExpression(returnStatement.Expression, semanticModel, continuationIndent, cancellationToken, out var expressionLines))
            {
                return false;
            }

            rewrittenLines = new List<string> { prefix };
            rewrittenLines.AddRange(expressionLines);
            rewrittenLines[rewrittenLines.Count - 1] += suffix;
            return true;
        }

        private static bool TryFormatExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            string indentation,
            CancellationToken cancellationToken,
            out List<string> rewrittenLines)
        {
            rewrittenLines = new List<string> { indentation + expression.ToString() };
            if (rewrittenLines[0].Length <= MaxLineLength)
            {
                return true;
            }

            if (TryFormatMethodChainExpression(expression, indentation, out rewrittenLines))
            {
                return true;
            }

            if (TryFormatInvocationArgumentsExpression(expression, indentation, out rewrittenLines))
            {
                return true;
            }

            if (TryFormatBinaryExpression(expression, semanticModel, indentation, cancellationToken, IsBooleanOperator, out rewrittenLines))
            {
                return true;
            }

            if (TryFormatBinaryExpression(expression, semanticModel, indentation, cancellationToken, IsArithmeticOperator, out rewrittenLines))
            {
                return true;
            }

            if (TryFormatStringLiteralExpression(expression, indentation, out rewrittenLines))
            {
                return true;
            }

            rewrittenLines = null;
            return false;
        }

        private static bool TryFormatMethodChainExpression(ExpressionSyntax expression, string indentation, out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            if (!TryGetMethodChain(expression, out var chainRoot, out var chainSegments))
            {
                return false;
            }

            var continuationIndent = indentation + GetIndentUnit(indentation);
            rewrittenLines = new List<string> { indentation + chainRoot };
            rewrittenLines.AddRange(chainSegments.Select(segment => continuationIndent + segment));
            return rewrittenLines.All(line => line.Length <= MaxLineLength);
        }

        private static bool TryFormatInvocationArgumentsExpression(ExpressionSyntax expression, string indentation, out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var invocation = expression as InvocationExpressionSyntax;
            if (invocation == null || invocation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var continuationIndent = indentation + GetIndentUnit(indentation);
            rewrittenLines = new List<string>
            {
                indentation + invocation.Expression + invocation.ArgumentList.OpenParenToken.Text,
            };

            for (var i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                var argument = invocation.ArgumentList.Arguments[i];
                var line = continuationIndent + argument;
                if (i < invocation.ArgumentList.Arguments.Count - 1)
                {
                    line += ",";
                }
                else
                {
                    line += invocation.ArgumentList.CloseParenToken.Text;
                }

                rewrittenLines.Add(line);
            }

            return rewrittenLines.All(line => line.Length <= MaxLineLength);
        }

        private static bool TryFormatBinaryExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            string indentation,
            CancellationToken cancellationToken,
            Func<SyntaxKind, bool> operatorPredicate,
            out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            if (!TryGetBinarySegments(expression, operatorPredicate, out var operands, out var operators))
            {
                return false;
            }

            if (operatorPredicate == IsArithmeticOperator)
            {
                var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
                if (typeInfo.Type?.SpecialType == SpecialType.System_String
                    || typeInfo.ConvertedType?.SpecialType == SpecialType.System_String)
                {
                    return false;
                }
            }

            var continuationIndent = operatorPredicate == IsArithmeticOperator
                ? indentation
                : indentation + GetIndentUnit(indentation);

            rewrittenLines = BuildOperatorSeparatedLines(indentation, continuationIndent, string.Empty, operands, operators);
            return rewrittenLines.All(line => line.Length <= MaxLineLength);
        }

        private static bool TryFormatStringLiteralExpression(ExpressionSyntax expression, string indentation, out List<string> rewrittenLines)
        {
            rewrittenLines = null;

            var literalExpression = expression as LiteralExpressionSyntax;
            if (literalExpression == null || !literalExpression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return false;
            }

            var segments = SplitStringLiteral(literalExpression.Token.ValueText, indentation.Length);
            if (segments.Count <= 1)
            {
                return false;
            }

            rewrittenLines = new List<string>();
            for (var i = 0; i < segments.Count; i++)
            {
                var line = indentation + SymbolDisplay.FormatLiteral(segments[i], quote: true);
                if (i < segments.Count - 1)
                {
                    line += " +";
                }

                rewrittenLines.Add(line);
            }

            return rewrittenLines.All(line => line.Length <= MaxLineLength);
        }

        private static List<string> BuildOperatorSeparatedLines(
            string firstLinePrefix,
            string continuationIndent,
            string suffix,
            IReadOnlyList<string> operands,
            IReadOnlyList<string> operators)
        {
            var rewrittenLines = new List<string>();
            for (var i = 0; i < operands.Count; i++)
            {
                var line = i == 0
                    ? firstLinePrefix + operands[i]
                    : continuationIndent + operands[i];

                if (i < operators.Count)
                {
                    line += " " + operators[i];
                }

                rewrittenLines.Add(line);
            }

            rewrittenLines[rewrittenLines.Count - 1] += suffix;
            return rewrittenLines;
        }

        private static bool TryGetMethodChain(ExpressionSyntax expression, out string chainRoot, out List<string> chainSegments)
        {
            chainRoot = null;
            chainSegments = new List<string>();

            ExpressionSyntax current = expression;
            while (true)
            {
                var invocation = current as InvocationExpressionSyntax;
                if (invocation?.Expression is MemberAccessExpressionSyntax invocationAccess)
                {
                    chainSegments.Add(invocationAccess.OperatorToken.Text + invocationAccess.Name + invocation.ArgumentList);
                    current = invocationAccess.Expression;
                    continue;
                }

                var memberAccess = current as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    chainSegments.Add(memberAccess.OperatorToken.Text + memberAccess.Name);
                    current = memberAccess.Expression;
                    continue;
                }

                break;
            }

            if (chainSegments.Count == 0)
            {
                return false;
            }

            chainSegments.Reverse();
            chainRoot = current.ToString();
            return true;
        }

        private static bool TryGetBinarySegments(
            ExpressionSyntax expression,
            Func<SyntaxKind, bool> operatorPredicate,
            out List<string> operands,
            out List<string> operators)
        {
            operands = new List<string>();
            operators = new List<string>();

            if (!(expression is BinaryExpressionSyntax binaryExpression) || !operatorPredicate(binaryExpression.Kind()))
            {
                return false;
            }

            CollectBinarySegments(expression, operatorPredicate, operands, operators);
            return operands.Count > 1;
        }

        private static void CollectBinarySegments(
            ExpressionSyntax expression,
            Func<SyntaxKind, bool> operatorPredicate,
            List<string> operands,
            List<string> operators)
        {
            var binaryExpression = expression as BinaryExpressionSyntax;
            if (binaryExpression == null || !operatorPredicate(binaryExpression.Kind()))
            {
                operands.Add(expression.ToString());
                return;
            }

            CollectBinarySegments(binaryExpression.Left, operatorPredicate, operands, operators);
            operators.Add(binaryExpression.OperatorToken.Text);
            CollectBinarySegments(binaryExpression.Right, operatorPredicate, operands, operators);
        }

        private static bool IsBooleanOperator(SyntaxKind syntaxKind)
        {
            return syntaxKind == SyntaxKind.LogicalAndExpression
                || syntaxKind == SyntaxKind.LogicalOrExpression;
        }

        private static bool IsArithmeticOperator(SyntaxKind syntaxKind)
        {
            return syntaxKind == SyntaxKind.AddExpression
                || syntaxKind == SyntaxKind.SubtractExpression
                || syntaxKind == SyntaxKind.MultiplyExpression
                || syntaxKind == SyntaxKind.DivideExpression
                || syntaxKind == SyntaxKind.ModuloExpression;
        }

        private static List<string> SplitStringLiteral(string valueText, int indentationLength)
        {
            var segments = new List<string>();
            var remainingText = valueText;
            while (remainingText.Length > 0)
            {
                var maxContentLength = MaxLineLength - indentationLength - (segments.Count == 0 ? 4 : 4);
                if (remainingText.Length <= MaxLineLength - indentationLength - 2)
                {
                    segments.Add(remainingText);
                    break;
                }

                var splitIndex = FindStringSplitIndex(remainingText, maxContentLength);
                segments.Add(remainingText.Substring(0, splitIndex));
                remainingText = remainingText.Substring(splitIndex);
            }

            return segments;
        }

        private static int FindStringSplitIndex(string text, int maxContentLength)
        {
            var splitIndex = Math.Min(text.Length, maxContentLength);
            for (var i = splitIndex; i > 0; i--)
            {
                if (char.IsWhiteSpace(text[i - 1]))
                {
                    return i;
                }
            }

            return splitIndex;
        }

        private static string GetLeadingWhitespace(string text)
        {
            var length = 0;
            while (length < text.Length && char.IsWhiteSpace(text[length]))
            {
                length++;
            }

            return text.Substring(0, length);
        }

        private static string GetIndentUnit(string indentation)
        {
            return indentation.IndexOf('\t') >= 0 ? "\t" : DefaultIndent;
        }

        private static string GetLineBreak(SourceText text, TextLine line)
        {
            var lineBreakSpan = TextSpan.FromBounds(line.Span.End, line.SpanIncludingLineBreak.End);
            return lineBreakSpan.Length == 0 ? Environment.NewLine : text.ToString(lineBreakSpan);
        }
    }
}
