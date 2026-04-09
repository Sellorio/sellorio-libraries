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
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseAnyInsteadOfCountOrLengthCodeFixProvider)), Shared]
    public class UseAnyInsteadOfCountOrLengthCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Use Any";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0007;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var binaryExpression = FindBinaryExpression(root, diagnostic.Location.SourceSpan);
            if (!TryCreateReplacement(binaryExpression, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => UseAnyAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static BinaryExpressionSyntax FindBinaryExpression(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?
                .AncestorsAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault();
        }

        private static async Task<Document> UseAnyAsync(
            Document document,
            TextSpan diagnosticSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var binaryExpression = FindBinaryExpression(root, diagnosticSpan);
            if (!TryCreateReplacement(binaryExpression, out var replacement))
            {
                return document;
            }

            var updatedExpression = replacement
                .WithLeadingTrivia(binaryExpression.GetLeadingTrivia())
                .WithTrailingTrivia(binaryExpression.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var updatedRoot = root.ReplaceNode(binaryExpression, updatedExpression);
            updatedRoot = AddSystemLinqUsingIfMissing(updatedRoot);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static bool TryCreateReplacement(
            BinaryExpressionSyntax binaryExpression,
            out ExpressionSyntax replacement)
        {
            replacement = null;
            if (binaryExpression == null)
            {
                return false;
            }

            var zeroOnLeft = IsZeroLiteral(binaryExpression.Left);
            var zeroOnRight = IsZeroLiteral(binaryExpression.Right);
            if (zeroOnLeft == zeroOnRight)
            {
                return false;
            }

            var memberAccess = zeroOnLeft
                ? binaryExpression.Right as MemberAccessExpressionSyntax
                : binaryExpression.Left as MemberAccessExpressionSyntax;

            if (!IsSupportedMemberAccess(memberAccess))
            {
                return false;
            }

            var anyInvocation = CreateAnyInvocation(memberAccess.Expression.WithoutTrivia());

            switch (binaryExpression.Kind())
            {
                case SyntaxKind.GreaterThanExpression:
                    if (!zeroOnRight)
                    {
                        return false;
                    }

                    replacement = anyInvocation;
                    return true;

                case SyntaxKind.NotEqualsExpression:
                    replacement = anyInvocation;
                    return true;

                case SyntaxKind.EqualsExpression:
                    replacement = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, anyInvocation);
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsSupportedMemberAccess(MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess == null)
            {
                return false;
            }

            var memberName = memberAccess.Name.Identifier.ValueText;
            return memberName == "Count" || memberName == "Length";
        }

        private static bool IsZeroLiteral(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax literal &&
                   literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
                   literal.Token.ValueText == "0";
        }

        private static InvocationExpressionSyntax CreateAnyInvocation(ExpressionSyntax expression)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParenthesizeIfNeeded(expression),
                    SyntaxFactory.IdentifierName("Any")));
        }

        private static ExpressionSyntax ParenthesizeIfNeeded(ExpressionSyntax expression)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.ThisExpression:
                case SyntaxKind.BaseExpression:
                case SyntaxKind.ObjectCreationExpression:
                case SyntaxKind.ImplicitObjectCreationExpression:
                case SyntaxKind.ParenthesizedExpression:
                    return expression;

                default:
                    return SyntaxFactory.ParenthesizedExpression(expression);
            }
        }

        private static SyntaxNode AddSystemLinqUsingIfMissing(SyntaxNode root)
        {
            if (!(root is CompilationUnitSyntax compilationUnit))
            {
                return root;
            }

            if (compilationUnit.Usings.Any(u => u.Alias == null && u.Name?.ToString() == "System.Linq"))
            {
                return root;
            }

            return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
        }
    }
}
