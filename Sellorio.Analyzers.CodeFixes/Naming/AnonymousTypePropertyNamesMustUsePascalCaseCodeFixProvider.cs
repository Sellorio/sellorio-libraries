using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnonymousTypePropertyNamesMustUsePascalCaseCodeFixProvider)), Shared]
    public class AnonymousTypePropertyNamesMustUsePascalCaseCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Use PascalCase name";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0020;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var declarator = FindAnonymousObjectMemberDeclarator(root, diagnostic.Location.SourceSpan);
            if (declarator?.NameEquals == null)
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

        private static AnonymousObjectMemberDeclaratorSyntax FindAnonymousObjectMemberDeclarator(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<AnonymousObjectMemberDeclaratorSyntax>().FirstOrDefault();
        }

        private static async Task<Document> UsePascalCaseNameAsync(
            Document document,
            TextSpan declarationSpan,
            System.Threading.CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var declarator = FindAnonymousObjectMemberDeclarator(root, declarationSpan);
            if (declarator?.NameEquals == null)
            {
                return document;
            }

            var explicitName = ToPascalCase(declarator.NameEquals.Name.Identifier.ValueText);
            var updatedDeclarator = declarator
                .WithNameEquals(declarator.NameEquals.WithName(SyntaxFactory.IdentifierName(explicitName)))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(declarator, updatedDeclarator);
            return document.WithSyntaxRoot(newRoot);
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
