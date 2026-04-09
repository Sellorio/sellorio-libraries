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

namespace Sellorio.Analyzers.CodeFixes.Naming
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnonymousTypePropertiesMustBeExplicitlyNamedCodeFixProvider)), Shared]
    public class AnonymousTypePropertiesMustBeExplicitlyNamedCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Add explicit PascalCase name";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0019;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var declarator = FindAnonymousObjectMemberDeclarator(root, diagnostic.Location.SourceSpan);
            if (declarator == null || declarator.NameEquals != null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (!TryGetImplicitPropertyName(declarator.Expression, semanticModel, context.CancellationToken, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => AddExplicitNameAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static AnonymousObjectMemberDeclaratorSyntax FindAnonymousObjectMemberDeclarator(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<AnonymousObjectMemberDeclaratorSyntax>().FirstOrDefault();
        }

        private static async Task<Document> AddExplicitNameAsync(
            Document document,
            TextSpan declarationSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var declarator = FindAnonymousObjectMemberDeclarator(root, declarationSpan);
            if (declarator == null || declarator.NameEquals != null)
            {
                return document;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetImplicitPropertyName(declarator.Expression, semanticModel, cancellationToken, out var propertyName))
            {
                return document;
            }

            var explicitName = ToPascalCase(propertyName);
            var updatedDeclarator = declarator
                .WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(explicitName)))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(declarator, updatedDeclarator);
            return document.WithSyntaxRoot(newRoot);
        }

        private static bool TryGetImplicitPropertyName(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out string propertyName)
        {
            expression = UnwrapExpression(expression);

            var symbol = semanticModel?.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (symbol != null)
            {
                propertyName = symbol.Name;
                return !string.IsNullOrEmpty(propertyName);
            }

            switch (expression)
            {
                case IdentifierNameSyntax identifierName:
                    propertyName = identifierName.Identifier.ValueText;
                    return true;

                case MemberAccessExpressionSyntax memberAccess:
                    propertyName = memberAccess.Name.Identifier.ValueText;
                    return true;

                case MemberBindingExpressionSyntax memberBinding:
                    propertyName = memberBinding.Name.Identifier.ValueText;
                    return true;

                case ConditionalAccessExpressionSyntax conditionalAccess
                    when conditionalAccess.WhenNotNull is MemberBindingExpressionSyntax conditionalMemberBinding:
                    propertyName = conditionalMemberBinding.Name.Identifier.ValueText;
                    return true;

                default:
                    propertyName = null;
                    return false;
            }
        }

        private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
        {
            while (true)
            {
                switch (expression)
                {
                    case ParenthesizedExpressionSyntax parenthesizedExpression:
                        expression = parenthesizedExpression.Expression;
                        continue;

                    case PostfixUnaryExpressionSyntax postfixUnaryExpression when postfixUnaryExpression.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                        expression = postfixUnaryExpression.Operand;
                        continue;

                    default:
                        return expression;
                }
            }
        }

        private static string ToPascalCase(string name)
        {
            var normalizedName = name;

            while (normalizedName.StartsWith("_", StringComparison.Ordinal))
            {
                normalizedName = normalizedName.Substring(1);
            }

            if (normalizedName.StartsWith("m_", StringComparison.Ordinal) && normalizedName.Length > 2)
            {
                normalizedName = normalizedName.Substring(2);
            }

            var parts = normalizedName
                .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part.Length > 0)
                .ToArray();

            if (parts.Length == 0)
            {
                return name;
            }

            return string.Concat(parts.Select(Capitalize));
        }

        private static string Capitalize(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            if (value.Length == 1)
            {
                return value.ToUpperInvariant();
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
