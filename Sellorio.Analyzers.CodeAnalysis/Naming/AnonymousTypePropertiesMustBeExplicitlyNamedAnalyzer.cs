using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnonymousTypePropertiesMustBeExplicitlyNamedAnalyzer : AnalyzerBase<AnonymousTypePropertiesMustBeExplicitlyNamedAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0019;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAnonymousObjectCreation, SyntaxKind.AnonymousObjectCreationExpression);
        }

        private void AnalyzeAnonymousObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var anonymousObject = (AnonymousObjectCreationExpressionSyntax)context.Node;

            foreach (var initializer in anonymousObject.Initializers)
            {
                // If NameEquals is null, the property name is inferred from the expression
                if (initializer.NameEquals == null)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        initializer.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
