using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Sellorio.Analyzers.CodeAnalysis.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseProtectedInternalAnalyzer : AnalyzerBase<DoNotUseProtectedInternalAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0006;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Field);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Event);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            if (context.Symbol.DeclaredAccessibility != Accessibility.ProtectedOrInternal)
            {
                return;
            }

            foreach (var syntaxRef in context.Symbol.DeclaringSyntaxReferences)
            {
                var node = syntaxRef.GetSyntax();
                var modifiers = GetModifiers(node);

                if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) &&
                    modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
                {
                    var protectedModifier = modifiers.First(m => m.IsKind(SyntaxKind.ProtectedKeyword));
                    var internalModifier = modifiers.First(m => m.IsKind(SyntaxKind.InternalKeyword));
                    var location = Location.Create(
                        node.SyntaxTree,
                        TextSpan.FromBounds(
                            Math.Min(protectedModifier.SpanStart, internalModifier.SpanStart),
                            Math.Max(protectedModifier.Span.End, internalModifier.Span.End)));

                    var diagnostic =
                        Diagnostic.Create(
                            DiagnosticDescriptor,
                            location);

                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }

        private static SyntaxTokenList GetModifiers(SyntaxNode node)
        {
            if (node is MemberDeclarationSyntax member)
            {
                return member.Modifiers;
            }

            if (node is AccessorDeclarationSyntax accessor)
            {
                return accessor.Modifiers;
            }

            // For variable declarators (fields/events), walk up to the containing member declaration
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is MemberDeclarationSyntax parentMember)
                {
                    return parentMember.Modifiers;
                }

                parent = parent.Parent;
            }

            return default;
        }
    }
}
