using System;
using System.Collections.Generic;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseParseInsteadOfConvertCodeFixProvider)), Shared]
    public class UseParseInsteadOfConvertCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Use Parse";

        private static readonly Dictionary<string, string> _convertMethodToType = new Dictionary<string, string>
        {
            { "ToBoolean", "bool" },
            { "ToByte", "byte" },
            { "ToSByte", "sbyte" },
            { "ToChar", "char" },
            { "ToInt16", "short" },
            { "ToUInt16", "ushort" },
            { "ToInt32", "int" },
            { "ToUInt32", "uint" },
            { "ToInt64", "long" },
            { "ToUInt64", "ulong" },
            { "ToSingle", "float" },
            { "ToDouble", "double" },
            { "ToDecimal", "decimal" },
            { "ToDateTime", "DateTime" }
        };

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0027;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);

            if (!TryCreateReplacement(invocation, out _, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: Title,
                    createChangedDocument: ct => UseParseAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static InvocationExpressionSyntax FindInvocation(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();
        }

        private static async Task<Document> UseParseAsync(
            Document document,
            TextSpan diagnosticSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return document;
            }

            var invocation = FindInvocation(root, diagnosticSpan);

            if (!TryCreateReplacement(invocation, out var replacement, out var typeName))
            {
                return document;
            }

            var updatedInvocation = replacement
                .WithLeadingTrivia(invocation.GetLeadingTrivia())
                .WithTrailingTrivia(invocation.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var updatedRoot = root.ReplaceNode(invocation, updatedInvocation);

            if (typeName == nameof(DateTime))
            {
                updatedRoot = AddSystemUsingIfMissing(updatedRoot);
            }

            return document.WithSyntaxRoot(updatedRoot);
        }

        private static bool TryCreateReplacement(
            InvocationExpressionSyntax invocation,
            out InvocationExpressionSyntax replacement,
            out string typeName)
        {
            replacement = null;
            typeName = null;

            if (!(invocation?.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return false;
            }

            if (!_convertMethodToType.TryGetValue(memberAccess.Name.Identifier.ValueText, out var replacementTypeName))
            {
                return false;
            }

            typeName = replacementTypeName;

            if (invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            replacement = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression(typeName + ".Parse"),
                invocation.ArgumentList);

            return true;
        }

        private static SyntaxNode AddSystemUsingIfMissing(SyntaxNode root)
        {
            if (!(root is CompilationUnitSyntax compilationUnit))
            {
                return root;
            }

            return
                compilationUnit.Usings.Any(u => u.Alias == null && u.Name?.ToString() == "System")
                    ? root
                    : compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
        }
    }
}
