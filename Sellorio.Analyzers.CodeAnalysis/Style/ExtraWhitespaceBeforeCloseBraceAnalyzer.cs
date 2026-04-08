using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExtraWhitespaceBeforeCloseBraceAnalyzer : AnalyzerBase<ExtraWhitespaceBeforeCloseBraceAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0013;

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

            var braceToken = context.Node.ChildTokens().FirstOrDefault(x => x.IsKind(SyntaxKind.CloseBraceToken));

            if (braceToken == null) // E.g. a record declaration with no extra definitions
            {
                return;
            }

            // Check if the close brace is the first non-trivia on its line
            // by verifying that the leading trivia contains a newline
            var leadingTrivia = braceToken.LeadingTrivia.ToString();
            if (!leadingTrivia.Contains("\n"))
            {
                // There is another token on the same line before the close brace
                return;
            }

            var braceLine = lines.GetLineFromPosition(braceToken.Span.End);

            // Ensure there is a previous line
            if (braceLine.LineNumber == 0)
            {
                return;
            }

            var nextLine = lines[braceLine.LineNumber - 1];

            var nextLineText = nextLine.ToString();

            // Check if previous line is empty or whitespace-only
            var trimmed = nextLineText.Trim();

            // Don't flag if the previous line is a comment
            if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.EndsWith("*/"))
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
