using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseAsOperatorAnalyzer : AnalyzerBase<DoNotUseAsOperatorAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0008;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                actionContext => actionContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, actionContext.Node.GetLocation())),
                SyntaxKind.AsExpression);
        }
    }
}
