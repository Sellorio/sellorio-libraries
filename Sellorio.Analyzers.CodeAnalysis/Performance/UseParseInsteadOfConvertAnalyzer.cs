using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseParseInsteadOfConvertAnalyzer : AnalyzerBase<UseParseInsteadOfConvertAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0027;

        private static readonly Dictionary<string, string> ConvertMethodToType = new Dictionary<string, string>
        {
            { "ToBoolean", "bool" },
            { "ToByte", "byte" },
            { "ToSByte", "sbyte" },
            { "ToChar", "char" },
            { "ToInt16", "short" },
            { "ToUInt16", "ushort" },
            { "ToInt32", "int" },
            { "ToUInt32", "uint" },
            { "ToInt64", "long" },
            { "ToUInt64", "ulong" },
            { "ToSingle", "float" },
            { "ToDouble", "double" },
            { "ToDecimal", "decimal" },
            { "ToDateTime", "DateTime" }
        };

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.ContainingType?.ToDisplayString() != "System.Convert")
            {
                return;
            }

            var methodName = methodSymbol.Name;

            if (!ConvertMethodToType.ContainsKey(methodName))
            {
                return;
            }

            if (methodSymbol.Parameters.Length != 1)
            {
                return;
            }

            var parameterType = methodSymbol.Parameters[0].Type;

            if (parameterType.SpecialType != SpecialType.System_String)
            {
                return;
            }

            var typeName = ConvertMethodToType[methodName];

            var diagnostic =
                Diagnostic.Create(
                    DiagnosticDescriptor,
                    invocation.GetLocation(),
                    typeName,
                    methodName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
