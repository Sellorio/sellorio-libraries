using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureOptionsLambdaParameterNameAnalyzer : AnalyzerBase<ConfigureOptionsLambdaParameterNameAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0028;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.ArgumentList == null)
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            // Check each argument
            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                var argument = invocation.ArgumentList.Arguments[i];
                
                // Get the parameter this argument corresponds to
                IParameterSymbol parameterSymbol = null;
                
                if (argument.NameColon != null)
                {
                    // Named argument
                    var parameterName = argument.NameColon.Name.Identifier.Text;
                    foreach (var param in methodSymbol.Parameters)
                    {
                        if (param.Name == parameterName)
                        {
                            parameterSymbol = param;
                            break;
                        }
                    }
                }
                else if (i < methodSymbol.Parameters.Length)
                {
                    // Positional argument
                    parameterSymbol = methodSymbol.Parameters[i];
                }

                if (parameterSymbol == null)
                {
                    continue;
                }

                // Check if parameter name contains "configure" or "options" (case insensitive)
                var paramName = parameterSymbol.Name;
                if (paramName.IndexOf("configure", StringComparison.OrdinalIgnoreCase) == -1 &&
                    paramName.IndexOf("options", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    continue;
                }

                // Check if the parameter is a delegate/function type
                if (parameterSymbol.Type.TypeKind != TypeKind.Delegate)
                {
                    continue;
                }

                var delegateType = parameterSymbol.Type as INamedTypeSymbol;
                if (delegateType == null)
                {
                    continue;
                }

                // Get the Invoke method to check parameter count
                var invokeMethod = delegateType.DelegateInvokeMethod;
                if (invokeMethod == null || invokeMethod.Parameters.Length != 1)
                {
                    // Only check lambdas with a single parameter
                    continue;
                }

                // Check if the argument is a lambda expression
                LambdaExpressionSyntax lambda = null;
                
                if (argument.Expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                {
                    lambda = parenthesizedLambda;
                }
                else if (argument.Expression is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    lambda = simpleLambda;
                }

                if (lambda == null)
                {
                    continue;
                }

                // Get the lambda parameter name
                string lambdaParamName = null;
                Location paramLocation = null;

                if (lambda is SimpleLambdaExpressionSyntax simple)
                {
                    lambdaParamName = simple.Parameter.Identifier.Text;
                    paramLocation = simple.Parameter.Identifier.GetLocation();
                }
                else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized)
                {
                    if (parenthesized.ParameterList.Parameters.Count == 1)
                    {
                        var param = parenthesized.ParameterList.Parameters[0];
                        lambdaParamName = param.Identifier.Text;
                        paramLocation = param.Identifier.GetLocation();
                    }
                }

                if (lambdaParamName == null)
                {
                    continue;
                }

                // Check if the parameter name is "o" or "options"
                if (lambdaParamName != "o" && lambdaParamName != "options")
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptor,
                        paramLocation,
                        lambdaParamName);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
