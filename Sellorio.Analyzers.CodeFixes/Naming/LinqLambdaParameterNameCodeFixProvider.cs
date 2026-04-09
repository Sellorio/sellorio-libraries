using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Naming
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LinqLambdaParameterNameCodeFixProvider)), Shared]
    public class LinqLambdaParameterNameCodeFixProvider : CodeFixProviderBase
    {
        private static readonly string[] _preferredNames = { "x", "y", "z", "a" };

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0029;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var parameter = FindParameter(root, diagnostic.Location.SourceSpan);
            if (parameter == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var expectedName = GetExpectedName(parameter, semanticModel, context.CancellationToken);
            if (string.IsNullOrEmpty(expectedName))
            {
                return;
            }

            var title = $"Rename lambda parameter to '{expectedName}'";
            context.RegisterCodeFix(
                CreateSolutionCodeAction(
                    context.Document,
                    title: title,
                    createChangedSolution: ct => RenameParameterAsync(context.Document, diagnostic.Location.SourceSpan, expectedName, ct),
                    equivalenceKey: title),
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
            string expectedName,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document.Project.Solution;
            }

            var parameter = FindParameter(root, parameterSpan);
            if (parameter == null)
            {
                return document.Project.Solution;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var parameterSymbol = semanticModel?.GetDeclaredSymbol(parameter, cancellationToken);
            if (parameterSymbol == null || parameterSymbol.Name == expectedName)
            {
                return document.Project.Solution;
            }

            return await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                parameterSymbol,
                new SymbolRenameOptions(false, false, false),
                expectedName,
                cancellationToken).ConfigureAwait(false);
        }

        private static string GetExpectedName(
            ParameterSyntax parameter,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel == null)
            {
                return null;
            }

            var lambda = parameter.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().FirstOrDefault();
            if (lambda == null)
            {
                return null;
            }

            var nestingLevel = CalculateNestingLevel(lambda, semanticModel, cancellationToken);
            return GetExpectedNameForLevel(nestingLevel);
        }

        private static int CalculateNestingLevel(
            LambdaExpressionSyntax lambda,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var level = 0;
            var current = lambda.Parent;

            while (current != null)
            {
                var currentLambda = current as LambdaExpressionSyntax;
                if (currentLambda != null && IsLinqLambda(currentLambda, semanticModel, cancellationToken))
                {
                    level++;
                }

                current = current.Parent;
            }

            return level;
        }

        private static bool IsLinqLambda(
            LambdaExpressionSyntax lambda,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var current = lambda.Parent;
            while (current != null && !(current is ArgumentSyntax))
            {
                current = current.Parent;
            }

            var argument = current as ArgumentSyntax;
            var invocation = argument?.Parent as ArgumentListSyntax;
            var invocationExpression = invocation?.Parent as InvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            return methodSymbol != null && IsSystemLinqExtensionMethod(methodSymbol);
        }

        private static bool IsSystemLinqExtensionMethod(IMethodSymbol methodSymbol)
        {
            if (!methodSymbol.IsExtensionMethod)
            {
                return false;
            }

            var containingType = methodSymbol.ContainingType;
            if (containingType == null)
            {
                return false;
            }

            var namespaceSymbol = containingType.ContainingNamespace;
            if (namespaceSymbol == null)
            {
                return false;
            }

            return namespaceSymbol.ToString() == "System.Linq";
        }

        private static string GetExpectedNameForLevel(int level)
        {
            if (level >= 0 && level < _preferredNames.Length)
            {
                return _preferredNames[level];
            }

            return null;
        }
    }
}
