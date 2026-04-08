using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MultilineAssignmentOrReturnMustStartOnNewLineAnalyzer : AnalyzerBase<MultilineAssignmentOrReturnMustStartOnNewLineAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0021;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAssignmentExpression, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeReturnStatement, SyntaxKind.ReturnStatement);
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var variableDeclaration = (VariableDeclarationSyntax)context.Node;

            foreach (var variable in variableDeclaration.Variables)
            {
                if (variable.Initializer == null)
                {
                    continue;
                }

                var equalsToken = variable.Initializer.EqualsToken;
                var value = variable.Initializer.Value;

                CheckMultilineExpression(context, equalsToken, value, "assignment");
            }
        }

        private void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            var equalsToken = assignment.OperatorToken;
            var value = assignment.Right;

            CheckMultilineExpression(context, equalsToken, value, "assignment");
        }

        private void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context)
        {
            var returnStatement = (ReturnStatementSyntax)context.Node;

            if (returnStatement.Expression == null)
            {
                return;
            }

            var returnKeyword = returnStatement.ReturnKeyword;
            var expression = returnStatement.Expression;

            CheckMultilineExpression(context, returnKeyword, expression, "return");
        }

        private void CheckMultilineExpression(
            SyntaxNodeAnalysisContext context,
            SyntaxToken operatorOrKeyword,
            ExpressionSyntax expression,
            string statementType)
        {
            if (expression == null)
            {
                return;
            }

            var sourceText = expression.SyntaxTree.GetText();

            // Get the line numbers
            var operatorLine = sourceText.Lines.GetLineFromPosition(operatorOrKeyword.Span.End).LineNumber;
            var expressionStartLine = sourceText.Lines.GetLineFromPosition(expression.SpanStart).LineNumber;

            var isMultiLine = IsRelevantPartMultiLine(sourceText, expression);

            // Check if expression starts on same line as operator/keyword
            var startsOnSameLine = operatorLine == expressionStartLine;

            if (isMultiLine && startsOnSameLine)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    expression.GetLocation(),
                    statementType);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsRelevantPartMultiLine(SourceText sourceText, ExpressionSyntax expression)
        {
            if (expression is SwitchExpressionSyntax switchExpression)
            {
                var governingExpressionStartLine = sourceText.Lines.GetLineFromPosition(switchExpression.GoverningExpression.SpanStart).LineNumber;
                var governingExpressionEndLine = sourceText.Lines.GetLineFromPosition(switchExpression.GoverningExpression.Span.End).LineNumber;
                return governingExpressionStartLine != governingExpressionEndLine;
            }

            var expressionStartLine = sourceText.Lines.GetLineFromPosition(expression.SpanStart).LineNumber;
            var expressionEndLine = sourceText.Lines.GetLineFromPosition(expression.Span.End).LineNumber;
            return expressionStartLine != expressionEndLine;
        }
    }
}
