using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Sellorio.Analyzers.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sellorio.Analyzers.CodeFixes.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnnecessaryAwaitCodeFixProvider)), Shared]
    public class UnnecessaryAwaitCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Simplify await";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0032;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            if (root == null || semanticModel == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var awaitExpression = FindAwaitExpression(root, diagnostic.Location.SourceSpan);

            if (!CanSimplify(awaitExpression, semanticModel, context.CancellationToken))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => SimplifyAwaitAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static AwaitExpressionSyntax FindAwaitExpression(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?
                .AncestorsAndSelf()
                .OfType<AwaitExpressionSyntax>()
                .FirstOrDefault(awaitExpression => awaitExpression.Span.IntersectsWith(span));
        }

        private static async Task<Document> SimplifyAwaitAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (root == null || semanticModel == null)
            {
                return document;
            }

            var awaitExpression = FindAwaitExpression(root, diagnosticSpan);

            if (awaitExpression == null)
            {
                return document;
            }

            if (TryGetFromResultReplacement(awaitExpression, semanticModel, cancellationToken, out var replacementExpression))
            {
                if (awaitExpression.Parent is ExpressionStatementSyntax expressionStatement)
                {
                    if (!IsValidStatementExpression(replacementExpression))
                    {
                        return document;
                    }

                    var updatedStatement = SyntaxFactory.ExpressionStatement(replacementExpression.WithoutTrivia())
                        .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);

                    return document.WithSyntaxRoot(root.ReplaceNode(expressionStatement, updatedStatement));
                }

                var updatedExpression = CreateReplacementExpression(awaitExpression, replacementExpression);
                return document.WithSyntaxRoot(root.ReplaceNode(awaitExpression, updatedExpression));
            }

            if (!IsCompletedTask(awaitExpression, semanticModel, cancellationToken))
            {
                return document;
            }

            switch (awaitExpression.Parent)
            {
                case ExpressionStatementSyntax expressionStatement:
                    return document.WithSyntaxRoot(root.RemoveNode(expressionStatement, SyntaxRemoveOptions.KeepNoTrivia));

                case ReturnStatementSyntax returnStatement:
                    var updatedReturn = SyntaxFactory.ReturnStatement()
                        .WithReturnKeyword(returnStatement.ReturnKeyword)
                        .WithSemicolonToken(returnStatement.SemicolonToken)
                        .WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(returnStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);

                    return document.WithSyntaxRoot(root.ReplaceNode(returnStatement, updatedReturn));

                default:
                    return document;
            }
        }

        private static bool CanSimplify(AwaitExpressionSyntax awaitExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (awaitExpression == null)
            {
                return false;
            }

            return
                TryGetFromResultReplacement(awaitExpression, semanticModel, cancellationToken, out var replacementExpression)
                    ? !(awaitExpression.Parent is ExpressionStatementSyntax) || IsValidStatementExpression(replacementExpression)
                    : IsCompletedTask(awaitExpression, semanticModel, cancellationToken) &&
                        (awaitExpression.Parent is ExpressionStatementSyntax || awaitExpression.Parent is ReturnStatementSyntax);
        }

        private static bool TryGetFromResultReplacement(
            AwaitExpressionSyntax awaitExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax replacementExpression)
        {
            replacementExpression = null;

            var awaitedExpression = Unwrap(awaitExpression.Expression);
            var invocationExpression = awaitedExpression as InvocationExpressionSyntax;

            if (invocationExpression == null || invocationExpression.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return false;
            }

            var containingType = methodSymbol.ContainingType?.ToDisplayString();

            if (methodSymbol.Name != "FromResult" ||
                containingType != "System.Threading.Tasks.Task" && containingType != "System.Threading.Tasks.ValueTask")
            {
                return false;
            }

            replacementExpression = invocationExpression.ArgumentList.Arguments[0].Expression;
            return true;
        }

        private static bool IsCompletedTask(AwaitExpressionSyntax awaitExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var awaitedExpression = Unwrap(awaitExpression.Expression);
            var memberAccessExpression = awaitedExpression as MemberAccessExpressionSyntax;

            if (memberAccessExpression == null)
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken);
            var propertySymbol = symbolInfo.Symbol as IPropertySymbol;

            if (propertySymbol == null)
            {
                return false;
            }

            var containingType = propertySymbol.ContainingType?.ToDisplayString();
            return propertySymbol.Name == "CompletedTask"
                && (containingType == "System.Threading.Tasks.Task" || containingType == "System.Threading.Tasks.ValueTask");
        }

        private static ExpressionSyntax CreateReplacementExpression(AwaitExpressionSyntax awaitExpression, ExpressionSyntax replacementExpression)
        {
            var expression = replacementExpression.WithoutTrivia();
            if (NeedsParentheses(expression))
            {
                expression = SyntaxFactory.ParenthesizedExpression(expression);
            }

            return expression
                .WithLeadingTrivia(awaitExpression.GetLeadingTrivia())
                .WithTrailingTrivia(awaitExpression.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                expression = parenthesizedExpression.Expression;
            }

            return expression;
        }

        private static bool NeedsParentheses(ExpressionSyntax expression)
        {
            return !(expression is IdentifierNameSyntax)
                && !(expression is GenericNameSyntax)
                && !(expression is LiteralExpressionSyntax)
                && !(expression is MemberAccessExpressionSyntax)
                && !(expression is InvocationExpressionSyntax)
                && !(expression is ObjectCreationExpressionSyntax)
                && !(expression is ImplicitObjectCreationExpressionSyntax)
                && !(expression is ElementAccessExpressionSyntax)
                && !(expression is ThisExpressionSyntax)
                && !(expression is BaseExpressionSyntax)
                && !(expression is DefaultExpressionSyntax)
                && !(expression is TypeOfExpressionSyntax)
                && !(expression is ParenthesizedExpressionSyntax);
        }

        private static bool IsValidStatementExpression(ExpressionSyntax expression)
        {
            return expression is InvocationExpressionSyntax
                || expression is ObjectCreationExpressionSyntax
                || expression is ImplicitObjectCreationExpressionSyntax
                || expression is AssignmentExpressionSyntax
                || expression.IsKind(SyntaxKind.PostIncrementExpression)
                || expression.IsKind(SyntaxKind.PostDecrementExpression)
                || expression.IsKind(SyntaxKind.PreIncrementExpression)
                || expression.IsKind(SyntaxKind.PreDecrementExpression);
        }
    }
}
