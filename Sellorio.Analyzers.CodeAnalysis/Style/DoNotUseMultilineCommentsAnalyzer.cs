using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseMultilineCommentsAnalyzer : AnalyzerBase<DoNotUseMultilineCommentsAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0025;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);

            // Get all trivia in the tree
            var triviaList = root.DescendantTrivia(descendIntoTrivia: true);

            foreach (var trivia in triviaList)
            {
                // Check for multiline comments (both /* */ and /** */ documentation style)
                if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        trivia.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
