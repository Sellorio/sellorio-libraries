using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NonPrivateFieldsAreNotAllowedAnalyzer : AnalyzerBase<NonPrivateFieldsAreNotAllowedAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0004;

        internal override Expression<Func<DiagnosticDescriptorValues>>[] AdditionalDescriptors =>
            new Expression<Func<DiagnosticDescriptorValues>>[]
            {
                () => Descriptors.SE0005
            };

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            var fieldSymbol = (IFieldSymbol)context.Symbol;

            if (fieldSymbol.DeclaredAccessibility != Accessibility.Private)
            {
                var diagnostic =
                    fieldSymbol.IsStatic && fieldSymbol.IsReadOnly
                        ? Diagnostic.Create(
                            AdditionalDiagnosticDescriptors[0],
                            fieldSymbol.Locations[0],
                            fieldSymbol.Name)
                        : Diagnostic.Create(
                            DiagnosticDescriptor,
                            fieldSymbol.Locations[0],
                            fieldSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
