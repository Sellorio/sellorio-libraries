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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TupleElementNamesMustUsePascalCaseCodeFixProvider)), Shared]
    public class TupleElementNamesMustUsePascalCaseCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Use PascalCase tuple element name";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0018;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];

            if (FindTupleExpressionName(root, diagnostic.Location.SourceSpan) == null && FindTupleTypeElement(root, diagnostic.Location.SourceSpan) == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => UsePascalCaseNameAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static IdentifierNameSyntax FindTupleExpressionName(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            var nameColon = token.Parent?.AncestorsAndSelf().OfType<NameColonSyntax>().FirstOrDefault();

            return nameColon?.Parent is ArgumentSyntax ? nameColon.Name : null;
        }

        private static TupleElementSyntax FindTupleTypeElement(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<TupleElementSyntax>().FirstOrDefault(element => !element.Identifier.IsKind(SyntaxKind.None));
        }

        private static async Task<Document> UsePascalCaseNameAsync(
            Document document,
            TextSpan declarationSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return document;
            }

            var tupleExpressionName = FindTupleExpressionName(root, declarationSpan);

            if (tupleExpressionName != null)
            {
                var explicitName = ToPascalCase(tupleExpressionName.Identifier.ValueText);
                var updatedName = tupleExpressionName
                    .WithIdentifier(SyntaxFactory.Identifier(tupleExpressionName.Identifier.LeadingTrivia, explicitName, tupleExpressionName.Identifier.TrailingTrivia))
                    .WithAdditionalAnnotations(Formatter.Annotation);

                var newRoot = root.ReplaceNode(tupleExpressionName, updatedName);
                return document.WithSyntaxRoot(newRoot);
            }

            var tupleTypeElement = FindTupleTypeElement(root, declarationSpan);

            if (tupleTypeElement == null)
            {
                return document;
            }

            var updatedElementName = ToPascalCase(tupleTypeElement.Identifier.ValueText);
            var updatedElement = tupleTypeElement
                .WithIdentifier(SyntaxFactory.Identifier(tupleTypeElement.Identifier.LeadingTrivia, updatedElementName, tupleTypeElement.Identifier.TrailingTrivia))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var updatedRoot = root.ReplaceNode(tupleTypeElement, updatedElement);
            return document.WithSyntaxRoot(updatedRoot);
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

            return parts.Length == 0 ? name : string.Concat(parts.Select(Capitalize));
        }

        private static string Capitalize(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            return
                value.Length == 1
                    ? value.ToUpperInvariant()
                    : (char.ToUpperInvariant(value[0]) + value.Substring(1));
        }
    }
}
