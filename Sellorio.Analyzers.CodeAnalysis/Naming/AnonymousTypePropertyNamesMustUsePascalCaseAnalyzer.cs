using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnonymousTypePropertyNamesMustUsePascalCaseAnalyzer : AnalyzerBase<AnonymousTypePropertyNamesMustUsePascalCaseAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0020;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAnonymousObjectCreation, SyntaxKind.AnonymousObjectCreationExpression);
        }

        private void AnalyzeAnonymousObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var anonymousObject = (AnonymousObjectCreationExpressionSyntax)context.Node;

            foreach (var initializer in anonymousObject.Initializers)
            {
                if (initializer.NameEquals == null)
                {
                    continue;
                }

                var propertyName = initializer.NameEquals.Name.Identifier.Text;
                var location = initializer.NameEquals.Name.GetLocation();

                if (!IsPascalCase(propertyName))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        location,
                        propertyName);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            // PascalCase starts with an uppercase letter
            if (!char.IsUpper(name[0]))
            {
                return false;
            }

            // Check if it doesn't contain underscores (common anti-pattern)
            if (name.Contains("_"))
            {
                return false;
            }

            return true;
        }
    }
}
