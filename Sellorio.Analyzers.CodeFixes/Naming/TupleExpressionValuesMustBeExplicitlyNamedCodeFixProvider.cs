using System;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;
using Sellorio.Analyzers.CodeFixes;

namespace Sellorio.Analyzers.CodeFixes.Naming
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TupleExpressionValuesMustBeExplicitlyNamedCodeFixProvider)), Shared]
    public class TupleExpressionValuesMustBeExplicitlyNamedCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Add explicit PascalCase tuple element name";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0016;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics[0];
            var argument = FindArgument(root, diagnostic.Location.SourceSpan);
            if (argument == null || argument.NameColon != null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (!TryGetImplicitTupleElementName(argument.Expression, semanticModel, context.CancellationToken, out _))
                return;

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => AddExplicitNameAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static ArgumentSyntax FindArgument(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();
        }

        private static async Task<Document> AddExplicitNameAsync(
            Document document,
            TextSpan declarationSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var argument = FindArgument(root, declarationSpan);
            if (argument == null || argument.NameColon != null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetImplicitTupleElementName(argument.Expression, semanticModel, cancellationToken, out var tupleElementName))
                return document;

            var explicitName = ToPascalCase(tupleElementName);
            var updatedArgument = argument
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(explicitName)))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(argument, updatedArgument);
            return document.WithSyntaxRoot(newRoot);
        }

        private static bool TryGetImplicitTupleElementName(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out string tupleElementName)
        {
            expression = UnwrapExpression(expression);

            var symbol = semanticModel?.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (symbol != null)
            {
                tupleElementName = symbol.Name;
                return !string.IsNullOrEmpty(tupleElementName);
            }

            switch (expression)
            {
                case IdentifierNameSyntax identifierName:
                    tupleElementName = identifierName.Identifier.ValueText;
                    return true;

                case MemberAccessExpressionSyntax memberAccess:
                    tupleElementName = memberAccess.Name.Identifier.ValueText;
                    return true;

                case MemberBindingExpressionSyntax memberBinding:
                    tupleElementName = memberBinding.Name.Identifier.ValueText;
                    return true;

                case ConditionalAccessExpressionSyntax conditionalAccess
                    when conditionalAccess.WhenNotNull is MemberBindingExpressionSyntax conditionalMemberBinding:
                    tupleElementName = conditionalMemberBinding.Name.Identifier.ValueText;
                    return true;

                default:
                    tupleElementName = null;
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
                return name;

            return string.Concat(parts.Select(Capitalize));
        }

        private static string Capitalize(string value)
        {
            if (value.Length == 0)
                return value;

            if (value.Length == 1)
                return value.ToUpperInvariant();

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
