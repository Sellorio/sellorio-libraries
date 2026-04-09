using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PrivatePropertiesAreNotAllowedAnalyzer : AnalyzerBase<PrivatePropertiesAreNotAllowedAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0003;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var propertySymbol = (IPropertySymbol)context.Symbol;

            // Only care about explicitly declared properties
            if (propertySymbol.DeclaredAccessibility == Accessibility.Private)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    propertySymbol.Locations[0],
                    propertySymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
