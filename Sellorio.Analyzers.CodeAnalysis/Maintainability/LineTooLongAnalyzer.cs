using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Sellorio.Analyzers.CodeAnalysis.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LineTooLongAnalyzer : AnalyzerBase<LineTooLongAnalyzer>
    {
        private const int MaxLineLength = 160;

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0026;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunctionStatement, SyntaxKind.LocalFunctionStatement);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
            {
                return;
            }

            AnalyzeMethodBody(
                context,
                methodDeclaration.Body,
                methodDeclaration.ExpressionBody,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.ValueText,
                GetExcludedSpans(methodDeclaration));
        }

        private void AnalyzeLocalFunctionStatement(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;

            if (localFunction.Body == null && localFunction.ExpressionBody == null)
            {
                return;
            }

            AnalyzeMethodBody(
                context,
                localFunction.Body,
                localFunction.ExpressionBody,
                localFunction.Identifier.GetLocation(),
                localFunction.Identifier.ValueText,
                Array.Empty<TextSpan>());
        }

        private void AnalyzeMethodBody(
            SyntaxNodeAnalysisContext context,
            BlockSyntax blockBody,
            ArrowExpressionClauseSyntax expressionBody,
            Location identifierLocation,
            string methodName,
            IReadOnlyList<TextSpan> excludedSpans)
        {
            var sourceText = context.Node.SyntaxTree.GetText(context.CancellationToken);
            var startLineNumber = 0;
            var endLineNumber = -1;

            if (blockBody != null)
            {
                startLineNumber = sourceText.Lines.GetLineFromPosition(blockBody.OpenBraceToken.SpanStart).LineNumber + 1;
                endLineNumber = sourceText.Lines.GetLineFromPosition(blockBody.CloseBraceToken.SpanStart).LineNumber - 1;
            }
            else if (expressionBody != null)
            {
                startLineNumber = sourceText.Lines.GetLineFromPosition(expressionBody.SpanStart).LineNumber;
                endLineNumber = sourceText.Lines.GetLineFromPosition(expressionBody.Span.End).LineNumber;
            }
            else
            {
                return;
            }

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                var line = sourceText.Lines[lineNumber];
                if (excludedSpans.Count > 0 && excludedSpans.Any(excludedSpan => excludedSpan.IntersectsWith(line.Span)))
                {
                    continue;
                }

                var lineText = line.ToString();
                if (lineText.Length <= MaxLineLength)
                {
                    continue;
                }

                var diagnosticSpan = GetDiagnosticSpan(line, lineText);
                if (diagnosticSpan.IsEmpty)
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    Location.Create(context.Node.SyntaxTree, diagnosticSpan),
                    methodName,
                    lineText.Length);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static TextSpan GetDiagnosticSpan(TextLine line, string lineText)
        {
            var startOffset = 0;
            while (startOffset < lineText.Length && char.IsWhiteSpace(lineText[startOffset]))
            {
                startOffset++;
            }

            var endOffset = lineText.Length - 1;
            while (endOffset >= startOffset && char.IsWhiteSpace(lineText[endOffset]))
            {
                endOffset--;
            }

            return endOffset < startOffset
                ? default
                : new TextSpan(line.Start + startOffset, endOffset - startOffset + 1);
        }

        private static IReadOnlyList<TextSpan> GetExcludedSpans(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Select(localFunction => localFunction.Span)
                .ToList();
        }
    }
}
