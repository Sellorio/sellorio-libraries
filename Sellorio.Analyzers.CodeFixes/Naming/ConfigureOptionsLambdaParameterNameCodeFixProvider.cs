using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Naming
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureOptionsLambdaParameterNameCodeFixProvider)), Shared]
    public class ConfigureOptionsLambdaParameterNameCodeFixProvider : CodeFixProviderBase
    {
        private const string Title = "Rename lambda parameter to 'o'";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0028;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics[0];
            var parameter = FindParameter(root, diagnostic.Location.SourceSpan);
            if (parameter == null)
                return;

            context.RegisterCodeFix(
                CreateSolutionCodeAction(
                    context.Document,
                    title: Title,
                    createChangedSolution: ct => RenameParameterAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static ParameterSyntax FindParameter(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
        }

        private static async Task<Solution> RenameParameterAsync(
            Document document,
            TextSpan parameterSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document.Project.Solution;

            var parameter = FindParameter(root, parameterSpan);
            if (parameter == null)
                return document.Project.Solution;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var parameterSymbol = semanticModel?.GetDeclaredSymbol(parameter, cancellationToken);
            if (parameterSymbol == null || parameterSymbol.Name == "o")
                return document.Project.Solution;

            return await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                parameterSymbol,
                new SymbolRenameOptions(false, false, false),
                "o",
                cancellationToken).ConfigureAwait(false);
        }
    }
}
