using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class TupleDeclarationValuesMustBeExplicitlyNamedAnalyzer : AnalyzerBase<TupleDeclarationValuesMustBeExplicitlyNamedAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0017;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                AnalyzeTupleType,
                SyntaxKind.TupleType);
        }

        private void AnalyzeTupleType(SyntaxNodeAnalysisContext context)
        {
            var tupleType = (TupleTypeSyntax)context.Node;

            foreach (var element in tupleType.Elements)
            {
                // If identifier is missing, it's unnamed
                if (element.Identifier.IsKind(SyntaxKind.None))
                {
                    var typeName = element.Type.ToString();

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        element.GetLocation(),
                        typeName);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
