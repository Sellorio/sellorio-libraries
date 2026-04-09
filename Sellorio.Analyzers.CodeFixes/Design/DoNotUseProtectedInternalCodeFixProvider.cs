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
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DoNotUseProtectedInternalCodeFixProvider)), Shared]
    public class DoNotUseProtectedInternalCodeFixProvider : CodeFixProviderBase
    {
        private const string RemoveProtectedTitle = "Remove 'protected' modifier";
        private const string RemoveInternalTitle = "Remove 'internal' modifier";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0006;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var memberDeclaration = FindMemberDeclaration(root, diagnostic.Location.SourceSpan);

            if (memberDeclaration == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: RemoveProtectedTitle,
                    createChangedDocument: ct => RemoveModifierAsync(context.Document, diagnostic.Location.SourceSpan, SyntaxKind.ProtectedKeyword, ct),
                    equivalenceKey: "RemoveProtected"),
                diagnostic);

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: RemoveInternalTitle,
                    createChangedDocument: ct => RemoveModifierAsync(context.Document, diagnostic.Location.SourceSpan, SyntaxKind.InternalKeyword, ct),
                    equivalenceKey: "RemoveInternal"),
                diagnostic);
        }

        private static MemberDeclarationSyntax FindMemberDeclaration(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        }

        private static async Task<Document> RemoveModifierAsync(
            Document document,
            TextSpan declarationSpan,
            SyntaxKind modifierKind,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = FindMemberDeclaration(root, declarationSpan);

            if (declaration == null)
            {
                return document;
            }

            var modifiers = declaration.Modifiers;

            var index = -1;
            for (var i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].IsKind(modifierKind))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return document;
            }

            SyntaxTokenList newModifiers;
            if (index == 0 && modifiers.Count > 1)
            {
                var tokenToRemove = modifiers[0];
                var nextToken = modifiers[1];
                var updatedNextToken = nextToken.WithLeadingTrivia(tokenToRemove.LeadingTrivia);
                newModifiers = modifiers.Replace(nextToken, updatedNextToken).RemoveAt(0);
            }
            else
            {
                newModifiers = modifiers.RemoveAt(index);
            }

            var newDeclaration = declaration.WithModifiers(newModifiers);
            var newRoot = root.ReplaceNode(declaration, newDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
