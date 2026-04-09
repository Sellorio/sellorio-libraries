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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonPrivateFieldsAreNotAllowedCodeFixProvider)), Shared]
    public class NonPrivateFieldsAreNotAllowedCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Convert to auto-property";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0004;

        internal override Expression<Func<DiagnosticDescriptorValues>>[] AdditionalDescriptors =>
            new Expression<Func<DiagnosticDescriptorValues>>[]
            {
                () => Descriptors.SE0005
            };

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var variableDeclarator = FindVariableDeclarator(root, diagnostic.Location.SourceSpan);
            var fieldDeclaration = variableDeclarator?.Parent?.Parent as FieldDeclarationSyntax;

            if (fieldDeclaration == null || fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => ConvertToAutoPropertyAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static VariableDeclaratorSyntax FindVariableDeclarator(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
        }

        private static async Task<Document> ConvertToAutoPropertyAsync(
            Document document,
            TextSpan declarationSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var variableDeclarator = FindVariableDeclarator(root, declarationSpan);
            var fieldDeclaration = variableDeclarator?.Parent?.Parent as FieldDeclarationSyntax;

            if (fieldDeclaration == null)
            {
                return document;
            }

            var propertyAnnotation = new SyntaxAnnotation();
            var propertyDeclaration = CreatePropertyDeclaration(fieldDeclaration, variableDeclarator)
                .WithAdditionalAnnotations(propertyAnnotation, Formatter.Annotation);

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (fieldDeclaration.Declaration.Variables.Count == 1)
            {
                editor.ReplaceNode(fieldDeclaration, propertyDeclaration);
            }
            else
            {
                var updatedFieldDeclaration = fieldDeclaration.WithDeclaration(
                    fieldDeclaration.Declaration.WithVariables(
                        SyntaxFactory.SeparatedList(fieldDeclaration.Declaration.Variables.Where(v => v != variableDeclarator))))
                    .WithAdditionalAnnotations(Formatter.Annotation);

                editor.ReplaceNode(fieldDeclaration, updatedFieldDeclaration);
                editor.InsertAfter(updatedFieldDeclaration, propertyDeclaration);
            }

            var changedDocument = editor.GetChangedDocument();
            var changedRoot = await changedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var changedPropertyDeclaration = changedRoot.GetAnnotatedNodes(propertyAnnotation).OfType<PropertyDeclarationSyntax>().FirstOrDefault();

            if (changedPropertyDeclaration == null)
            {
                return changedDocument;
            }

            var semanticModel = await changedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = semanticModel.GetDeclaredSymbol(changedPropertyDeclaration, cancellationToken);

            if (propertySymbol == null)
            {
                return changedDocument;
            }

            var pascalCaseName = ToPascalCase(variableDeclarator.Identifier.ValueText);
            if (propertySymbol.Name == pascalCaseName)
            {
                return changedDocument;
            }

            var renamedSolution = await Renamer.RenameSymbolAsync(
                changedDocument.Project.Solution,
                propertySymbol,
                new SymbolRenameOptions(false, false, false),
                pascalCaseName,
                cancellationToken).ConfigureAwait(false);

            return renamedSolution.GetDocument(changedDocument.Id);
        }

        private static PropertyDeclarationSyntax CreatePropertyDeclaration(
            FieldDeclarationSyntax fieldDeclaration,
            VariableDeclaratorSyntax variableDeclarator)
        {
            var modifiers = SyntaxFactory.TokenList(
                fieldDeclaration.Modifiers.Where(
                    modifier =>
                        !modifier.IsKind(SyntaxKind.ReadOnlyKeyword)
                        && !modifier.IsKind(SyntaxKind.VolatileKeyword)));

            var accessors = new[]
            {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            };

            if (!fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                accessors = accessors.Concat(
                    new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    }).ToArray();
            }

            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    fieldDeclaration.AttributeLists,
                    modifiers,
                    fieldDeclaration.Declaration.Type,
                    null,
                    SyntaxFactory.Identifier(variableDeclarator.Identifier.Text),
                    CreateAccessorList(accessors))
                .WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia());

            if (variableDeclarator.Initializer != null)
            {
                propertyDeclaration = propertyDeclaration
                    .WithInitializer(variableDeclarator.Initializer)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            return propertyDeclaration.WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia());
        }

        private static AccessorListSyntax CreateAccessorList(AccessorDeclarationSyntax[] accessors)
        {
            var spacedAccessors = accessors
                .Select((accessor, index) =>
                    accessor.WithSemicolonToken(
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.SemicolonToken,
                            index < accessors.Length - 1
                                ? SyntaxFactory.TriviaList(SyntaxFactory.Space)
                                : SyntaxFactory.TriviaList())))
                .ToArray();

            return SyntaxFactory.AccessorList(
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(SyntaxFactory.Space),
                    SyntaxKind.OpenBraceToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                SyntaxFactory.List(spacedAccessors),
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(SyntaxFactory.Space),
                    SyntaxKind.CloseBraceToken,
                    SyntaxFactory.TriviaList()));
        }

        private static string ToPascalCase(string fieldName)
        {
            var normalizedName = fieldName;

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
                return fieldName;
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
