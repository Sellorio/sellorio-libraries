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
            var expressionStartLine = GetLineNumber(sourceText, expression.SpanStart);
            var expressionEndLine = GetRelevantExpressionEndLine(sourceText, expression);
            return expressionStartLine != expressionEndLine;
        }

        private static int GetRelevantExpressionEndLine(SourceText sourceText, ExpressionSyntax expression)
        {
            var switchExpression = expression as SwitchExpressionSyntax;

            if (switchExpression != null)
            {
                return GetLineNumber(sourceText, switchExpression.GoverningExpression.Span.End);
            }

            var simpleLambda = expression as SimpleLambdaExpressionSyntax;

            if (simpleLambda != null && simpleLambda.Body is BlockSyntax)
            {
                return GetLineNumber(sourceText, simpleLambda.ArrowToken.Span.End);
            }

            var parenthesizedLambda = expression as ParenthesizedLambdaExpressionSyntax;

            if (parenthesizedLambda != null && parenthesizedLambda.Body is BlockSyntax)
            {
                return GetLineNumber(sourceText, parenthesizedLambda.ArrowToken.Span.End);
            }

            var anonymousMethod = expression as AnonymousMethodExpressionSyntax;

            if (anonymousMethod != null && anonymousMethod.Block != null)
            {
                return GetLineNumber(sourceText, anonymousMethod.Block.GetFirstToken().GetPreviousToken().Span.End);
            }

            var objectCreation = expression as ObjectCreationExpressionSyntax;

            if (objectCreation != null && objectCreation.Initializer != null)
            {
                return GetLineNumber(sourceText, objectCreation.Initializer.OpenBraceToken.GetPreviousToken().Span.End);
            }

            var implicitObjectCreation = expression as ImplicitObjectCreationExpressionSyntax;

            if (implicitObjectCreation != null && implicitObjectCreation.Initializer != null)
            {
                return GetLineNumber(sourceText, implicitObjectCreation.Initializer.OpenBraceToken.GetPreviousToken().Span.End);
            }

            var arrayCreation = expression as ArrayCreationExpressionSyntax;

            if (arrayCreation != null && arrayCreation.Initializer != null)
            {
                return GetLineNumber(sourceText, arrayCreation.Initializer.OpenBraceToken.GetPreviousToken().Span.End);
            }

            var implicitArrayCreation = expression as ImplicitArrayCreationExpressionSyntax;

            if (implicitArrayCreation != null && implicitArrayCreation.Initializer != null)
            {
                return GetLineNumber(sourceText, implicitArrayCreation.Initializer.OpenBraceToken.GetPreviousToken().Span.End);
            }

            var stackAllocArrayCreation = expression as StackAllocArrayCreationExpressionSyntax;

            if (stackAllocArrayCreation != null && stackAllocArrayCreation.Initializer != null)
            {
                return GetLineNumber(sourceText, stackAllocArrayCreation.Initializer.OpenBraceToken.GetPreviousToken().Span.End);
            }

            var anonymousObjectCreation = expression as AnonymousObjectCreationExpressionSyntax;

            if (anonymousObjectCreation != null)
            {
                return GetLineNumber(sourceText, anonymousObjectCreation.OpenBraceToken.GetPreviousToken().Span.End);
            }

            var withExpression = expression as WithExpressionSyntax;

            if (withExpression != null)
            {
                return GetLineNumber(sourceText, withExpression.Initializer.OpenBraceToken.GetPreviousToken().Span.End);
            }

            return GetLineNumber(sourceText, expression.Span.End);
        }

        private static int GetLineNumber(SourceText sourceText, int position)
        {
            return sourceText.Lines.GetLineFromPosition(position).LineNumber;
        }
    }
}
