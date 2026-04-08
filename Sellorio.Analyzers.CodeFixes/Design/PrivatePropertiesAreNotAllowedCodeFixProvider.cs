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

namespace Sellorio.Analyzers.CodeFixes.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrivatePropertiesAreNotAllowedCodeFixProvider)), Shared]
    public class PrivatePropertiesAreNotAllowedCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Convert to field";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0003;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var propertyDeclaration = FindPropertyDeclaration(root, diagnostic.Location.SourceSpan);

            if (!CanConvertToField(propertyDeclaration))
                return;

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => ConvertToFieldAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static PropertyDeclarationSyntax FindPropertyDeclaration(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        }

        private static bool CanConvertToField(PropertyDeclarationSyntax propertyDeclaration) =>
            propertyDeclaration != null
            && (IsAutoProperty(propertyDeclaration) || IsExpressionBodiedProperty(propertyDeclaration));

        private static bool IsAutoProperty(PropertyDeclarationSyntax propertyDeclaration) =>
            propertyDeclaration.ExpressionBody == null
            && propertyDeclaration.AccessorList != null
            && propertyDeclaration.AccessorList.Accessors.Count > 0
            && propertyDeclaration.AccessorList.Accessors.All(
                accessor =>
                    accessor.Body == null
                    && accessor.ExpressionBody == null
                    && accessor.SemicolonToken.IsKind(SyntaxKind.SemicolonToken));

        private static bool IsExpressionBodiedProperty(PropertyDeclarationSyntax propertyDeclaration) =>
            propertyDeclaration.AccessorList == null
            && propertyDeclaration.ExpressionBody != null;

        private static async Task<Document> ConvertToFieldAsync(
            Document document,
            TextSpan declarationSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var propertyDeclaration = FindPropertyDeclaration(root, declarationSpan);

            if (!CanConvertToField(propertyDeclaration))
                return document;

            var fieldDeclaration = CreateFieldDeclaration(propertyDeclaration)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(propertyDeclaration, fieldDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        private static FieldDeclarationSyntax CreateFieldDeclaration(PropertyDeclarationSyntax propertyDeclaration)
        {
            var modifiers = propertyDeclaration.Modifiers;
            if (ShouldBeReadOnly(propertyDeclaration) && !modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            }

            var initializer = propertyDeclaration.Initializer;
            if (initializer == null && propertyDeclaration.ExpressionBody != null)
            {
                initializer = SyntaxFactory.EqualsValueClause(propertyDeclaration.ExpressionBody.Expression);
            }

            var variableDeclarator = SyntaxFactory.VariableDeclarator(propertyDeclaration.Identifier);
            if (initializer != null)
            {
                variableDeclarator = variableDeclarator.WithInitializer(initializer);
            }

            return SyntaxFactory.FieldDeclaration(
                    propertyDeclaration.AttributeLists,
                    modifiers,
                    SyntaxFactory.VariableDeclaration(
                        propertyDeclaration.Type,
                        SyntaxFactory.SingletonSeparatedList(variableDeclarator)))
                .WithLeadingTrivia(propertyDeclaration.GetLeadingTrivia())
                .WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private static bool ShouldBeReadOnly(PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.ExpressionBody != null)
                return true;

            return propertyDeclaration.AccessorList != null
                && propertyDeclaration.AccessorList.Accessors.All(
                    accessor =>
                        accessor.Kind() != SyntaxKind.SetAccessorDeclaration
                        && accessor.Kind() != SyntaxKind.InitAccessorDeclaration);
        }
    }
}
