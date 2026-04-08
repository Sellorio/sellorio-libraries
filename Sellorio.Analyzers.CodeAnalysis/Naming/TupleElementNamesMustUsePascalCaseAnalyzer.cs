using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TupleElementNamesMustUsePascalCaseAnalyzer : AnalyzerBase<TupleElementNamesMustUsePascalCaseAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0018;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeTupleExpression, SyntaxKind.TupleExpression);
            context.RegisterSyntaxNodeAction(AnalyzeTupleType, SyntaxKind.TupleType);
        }

        private void AnalyzeTupleExpression(SyntaxNodeAnalysisContext context)
        {
            var tuple = (TupleExpressionSyntax)context.Node;

            foreach (var argument in tuple.Arguments)
            {
                // Check if the argument has an explicit name
                if (argument.NameColon != null)
                {
                    var name = argument.NameColon.Name.Identifier.Text;
                    if (!IsPascalCase(name))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptor,
                            argument.NameColon.Name.GetLocation(),
                            name);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private void AnalyzeTupleType(SyntaxNodeAnalysisContext context)
        {
            var tupleType = (TupleTypeSyntax)context.Node;

            foreach (var element in tupleType.Elements)
            {
                // Check if the element has an explicit name
                if (!element.Identifier.IsKind(SyntaxKind.None))
                {
                    var name = element.Identifier.Text;
                    if (!IsPascalCase(name))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptor,
                            element.Identifier.GetLocation(),
                            name);

                        context.ReportDiagnostic(diagnostic);
                    }
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
