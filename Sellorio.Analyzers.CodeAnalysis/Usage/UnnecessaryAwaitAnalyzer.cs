using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryAwaitAnalyzer : AnalyzerBase<UnnecessaryAwaitAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0032;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
        }

        private void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
        {
            var awaitExpression = (AwaitExpressionSyntax)context.Node;

            var awaitedExpression = awaitExpression.Expression;
            while (awaitedExpression is ParenthesizedExpressionSyntax paren)
            {
                awaitedExpression = paren.Expression;
            }

            var name = GetAlreadyCompletedTaskName(context, awaitedExpression);
            if (name == null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, awaitExpression.GetLocation(), name));
        }

        private static string GetAlreadyCompletedTaskName(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            // Task.FromResult(...), ValueTask.FromResult(...)
            if (expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    var containingType = methodSymbol.ContainingType?.ToDisplayString();
                    var methodName = methodSymbol.Name;

                    if (containingType == "System.Threading.Tasks.Task" && methodName == "FromResult")
                    {
                        return $"Task.{methodName}";
                    }

                    if (containingType == "System.Threading.Tasks.ValueTask" && methodName == "FromResult")
                    {
                        return $"ValueTask.{methodName}";
                    }
                }
            }

            // Task.CompletedTask, ValueTask.CompletedTask
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    var containingType = propertySymbol.ContainingType?.ToDisplayString();
                    var propName = propertySymbol.Name;

                    if (containingType == "System.Threading.Tasks.Task" && propName == "CompletedTask")
                    {
                        return "Task.CompletedTask";
                    }

                    if (containingType == "System.Threading.Tasks.ValueTask" && propName == "CompletedTask")
                    {
                        return "ValueTask.CompletedTask";
                    }
                }
            }

            return null;
        }
    }
}
