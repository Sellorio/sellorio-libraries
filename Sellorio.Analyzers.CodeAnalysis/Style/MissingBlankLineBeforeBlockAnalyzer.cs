using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingBlankLineBeforeBlockAnalyzer : AnalyzerBase<MissingBlankLineBeforeBlockAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0015;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                AnalyzeBlockStatement,
                SyntaxKind.IfStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachKeyword,
                SyntaxKind.ForEachVariableStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.LockStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.TryStatement);
        }

        private void AnalyzeBlockStatement(SyntaxNodeAnalysisContext context)
        {
            if (!TryGetFirstTokenIfBlockIsInScopeForAnalyzer(context.Node, out var firstToken))
            {
                return;
            }

            if (HasBlankLineBefore(firstToken.Value))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(DiagnosticDescriptor,
                firstToken.Value.GetLocation()));
        }

        private static bool HasBlankLineBefore(SyntaxToken token)
        {
            var syntaxTree = token.Parent.SyntaxTree;
            var text = syntaxTree.GetText();
            var startLine = text.Lines.GetLineFromPosition(token.Span.Start).LineNumber;
            var currentLine = startLine;

            while (true)
            {
                // if is at the start of code (i.e. top level statements in Program.cs)
                if (currentLine <= 0)
                {
                    return true;
                }

                var lineText = text.Lines[currentLine - 1].ToString().Trim();

                // if the line is a comment, skip it and check the line above it
                if (lineText.StartsWith("//"))
                {
                    currentLine--;
                    continue;
                }

                // blank line! woo!
                if (string.IsNullOrEmpty(lineText))
                {
                    return true;
                }

                // if the line contains an open brace, that's acceptable too
                if (lineText.EndsWith("{"))
                {
                    return true;
                }

                // bad! this line is neither a comment nor a blank line! developer is evil!
                return false;
            }
        }

        private static bool TryGetFirstTokenIfBlockIsInScopeForAnalyzer(SyntaxNode blockStatement, out SyntaxToken? token)
        {
            switch (blockStatement)
            {
                case IfStatementSyntax ifStmt when ifStmt.Parent == null || !ifStmt.Parent.IsKind(SyntaxKind.ElseClause):
                case WhileStatementSyntax _:
                case ForStatementSyntax _:
                case ForEachStatementSyntax _:
                case ForEachVariableStatementSyntax _:
                case UsingStatementSyntax _:
                case LockStatementSyntax _:
                case DoStatementSyntax _:
                case TryStatementSyntax _:
                    token = blockStatement.GetFirstToken();
                    return true;
                default:
                    token = null;
                    return false;
            }
        }
    }
}
