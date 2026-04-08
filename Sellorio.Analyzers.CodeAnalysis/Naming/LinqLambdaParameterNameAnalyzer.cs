using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LinqLambdaParameterNameAnalyzer : AnalyzerBase<LinqLambdaParameterNameAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0029;

        private static readonly string[] PreferredNames = { "x", "y", "z", "a" };

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null || !IsSystemLinqExtensionMethod(methodSymbol))
            {
                return;
            }

            // Find the nesting level based on parent lambda expressions
            int nestingLevel = CalculateNestingLevel(context, invocation);

            // Check each argument that contains a lambda
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                CheckLambdaArgument(context, argument, nestingLevel);
            }
        }

        private int CalculateNestingLevel(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            int level = 0;
            var current = node.Parent;

            while (current != null)
            {
                // Check if we're inside a lambda expression
                if (current is LambdaExpressionSyntax)
                {
                    // Check if this lambda is part of a LINQ method call
                    var lambdaParent = current.Parent;
                    while (lambdaParent != null && !(lambdaParent is ArgumentSyntax))
                    {
                        lambdaParent = lambdaParent.Parent;
                    }

                    if (lambdaParent is ArgumentSyntax argument)
                    {
                        var argumentList = argument.Parent as ArgumentListSyntax;
                        if (argumentList?.Parent is InvocationExpressionSyntax parentInvocation)
                        {
                            // Check if the parent invocation is a LINQ method
                            var parentSymbolInfo = context.SemanticModel.GetSymbolInfo(parentInvocation);
                            var parentMethodSymbol = parentSymbolInfo.Symbol as IMethodSymbol;

                            if (parentMethodSymbol != null && IsSystemLinqExtensionMethod(parentMethodSymbol))
                            {
                                level++;
                            }
                        }
                    }
                }

                current = current.Parent;
            }

            return level;
        }

        private bool IsSystemLinqExtensionMethod(IMethodSymbol methodSymbol)
        {
            if (!methodSymbol.IsExtensionMethod)
            {
                return false;
            }

            var containingType = methodSymbol.ContainingType;
            if (containingType == null)
            {
                return false;
            }

            // Check if it's in System.Linq namespace
            var namespaceSymbol = containingType.ContainingNamespace;
            if (namespaceSymbol == null)
            {
                return false;
            }

            return namespaceSymbol.ToString() == "System.Linq";
        }

        private void CheckLambdaArgument(SyntaxNodeAnalysisContext context, ArgumentSyntax argument, int nestingLevel)
        {
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
                return;
            }

            // Get the lambda parameter name
            var parameters = GetLambdaParameters(lambda);

            // Only check single-parameter lambdas
            if (parameters.Count != 1)
            {
                return;
            }

            var (paramName, paramLocation) = parameters[0];

            // Determine expected name based on nesting level
            string expectedName = GetExpectedNameForLevel(nestingLevel);

            // If expectedName is null, we're beyond the 4th level and no checking is needed
            if (expectedName == null)
            {
                return;
            }

            if (paramName != expectedName)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptor,
                    paramLocation,
                    paramName,
                    expectedName);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private List<(string Name, Location Location)> GetLambdaParameters(LambdaExpressionSyntax lambda)
        {
            var result = new List<(string, Location)>();

            if (lambda is SimpleLambdaExpressionSyntax simple)
            {
                result.Add((simple.Parameter.Identifier.Text, simple.Parameter.Identifier.GetLocation()));
            }
            else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized)
            {
                foreach (var param in parenthesized.ParameterList.Parameters)
                {
                    result.Add((param.Identifier.Text, param.Identifier.GetLocation()));
                }
            }

            return result;
        }

        private string GetExpectedNameForLevel(int level)
        {
            if (level >= 0 && level < PreferredNames.Length)
            {
                return PreferredNames[level];
            }

            // Beyond 'a', no further checking
            return null;
        }
    }
}
