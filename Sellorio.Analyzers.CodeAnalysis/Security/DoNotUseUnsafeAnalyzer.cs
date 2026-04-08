using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Security
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseUnsafeAnalyzer : AnalyzerBase<DoNotUseUnsafeAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0001;

        protected override void RegisterActions(AnalysisContext context)
        {
            // Register for syntax nodes that can contain 'unsafe'
            context.RegisterSyntaxNodeAction(AnalyzeUnsafeNode, SyntaxKind.UnsafeStatement);
            context.RegisterSyntaxNodeAction(AnalyzeUnsafeNode, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeUnsafeNode, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeUnsafeNode, SyntaxKind.StructDeclaration);
        }

        private void AnalyzeUnsafeNode(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;

            // Check for explicit unsafe blocks: `unsafe { ... }`
            if (node is UnsafeStatementSyntax unsafeStatement)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    unsafeStatement.UnsafeKeyword.GetLocation());

                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Check modifiers: `unsafe void Foo()`, `unsafe class`, etc.
            if (node is MemberDeclarationSyntax member)
            {
                foreach (var modifier in member.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.UnsafeKeyword))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptor,
                            modifier.GetLocation());

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
