using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExtraWhitespaceAfterOpenBraceAnalyzer : AnalyzerBase<ExtraWhitespaceAfterOpenBraceAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0012;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeOpenBrace, SyntaxKind.Block);
            context.RegisterSyntaxNodeAction(AnalyzeOpenBrace, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeOpenBrace, SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeOpenBrace, SyntaxKind.InterfaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeOpenBrace, SyntaxKind.StructDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeOpenBrace, SyntaxKind.RecordDeclaration);
        }

        private void AnalyzeOpenBrace(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.Node.SyntaxTree;
            var text = syntaxTree.GetText(context.CancellationToken);
            var lines = text.Lines;

            var braceToken = context.Node.DescendantTokens().FirstOrDefault(x => x.IsKind(SyntaxKind.OpenBraceToken));

            if (braceToken == null) // E.g. a record declaration with no extra definitions
            {
                return;
            }

            // Check if the open brace is the last non-trivia on its line
            // by verifying that the trailing trivia contains a newline
            var trailingTrivia = braceToken.TrailingTrivia.ToString();
            if (!trailingTrivia.Contains("\n"))
            {
                // There is another token on the same line after the open brace
                return;
            }

            var braceLine = lines.GetLineFromPosition(braceToken.Span.End);

            // Ensure there is a next line
            if (braceLine.LineNumber + 1 >= lines.Count)
            {
                return;
            }

            var nextLine = lines[braceLine.LineNumber + 1];

            var nextLineText = nextLine.ToString();

            // Check if next line is empty or whitespace-only
            var trimmed = nextLineText.Trim();

            // Don't flag if the next line is a comment
            if (trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
            {
                return;
            }

            if (string.IsNullOrEmpty(trimmed))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    braceToken.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
