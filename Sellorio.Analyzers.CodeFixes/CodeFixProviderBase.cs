using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes
{
    public abstract class CodeFixProviderBase : CodeFixProvider
    {
        internal abstract Expression<Func<DiagnosticDescriptorValues>> Descriptor { get; }
        internal virtual Expression<Func<DiagnosticDescriptorValues>>[] AdditionalDescriptors => Array.Empty<Expression<Func<DiagnosticDescriptorValues>>>();

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.CreateRange(new[] { Descriptor }.Concat(AdditionalDescriptors).Select(GetDiagnosticId));

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        protected static CodeAction CreateDocumentCodeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey)
        {
            return CodeAction.Create(
                title,
                ct => FormatDocumentAsync(createChangedDocument, ct),
                equivalenceKey);
        }

        protected static CodeAction CreateSolutionCodeAction(
            Document document,
            string title,
            Func<CancellationToken, Task<Solution>> createChangedSolution,
            string equivalenceKey)
        {
            return CodeAction.Create(
                title,
                ct => FormatChangedDocumentAsync(document.Id, createChangedSolution, ct),
                equivalenceKey);
        }

        private static async Task<Document> FormatDocumentAsync(
            Func<CancellationToken, Task<Document>> createChangedDocument,
            CancellationToken cancellationToken)
        {
            var document = await createChangedDocument(cancellationToken).ConfigureAwait(false);
            return await Formatter.FormatAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Solution> FormatChangedDocumentAsync(
            DocumentId documentId,
            Func<CancellationToken, Task<Solution>> createChangedSolution,
            CancellationToken cancellationToken)
        {
            var solution = await createChangedSolution(cancellationToken).ConfigureAwait(false);
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return solution;
            }

            var formattedDocument = await Formatter.FormatAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
            return formattedDocument.Project.Solution;
        }

        private static string GetDiagnosticId(Expression<Func<DiagnosticDescriptorValues>> descriptor)
        {
            var memberExpression = (MemberExpression)descriptor.Body;
            var propertyInfo = (PropertyInfo)memberExpression.Member;
            return propertyInfo.Name;
        }
    }
}
