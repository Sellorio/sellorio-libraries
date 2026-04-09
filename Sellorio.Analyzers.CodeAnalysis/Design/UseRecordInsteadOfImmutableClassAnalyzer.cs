using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseRecordInsteadOfImmutableClassAnalyzer : AnalyzerBase<UseRecordInsteadOfImmutableClassAnalyzer>
    {
        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0011;

        protected override void RegisterActions(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var parseOptions = classDecl.SyntaxTree.Options as CSharpParseOptions;

            if (!SupportsRecords(parseOptions))
            {
                return;
            }

            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            if (!HasSupportedBaseType(classSymbol))
            {
                return;
            }

            var properties = classDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .ToList();

            if (properties.Count == 0)
            {
                return;
            }

            // Must all be read-only
            if (!properties.All(IsReadOnlyProperty))
            {
                return;
            }

            // Collect constructor parameters (normal ctors)
            var constructors =
                classDecl.Members
                    .OfType<ConstructorDeclarationSyntax>()
                    .ToList();

            var allParameters = new List<IParameterSymbol>();

            foreach (var ctor in constructors)
            {
                var parameters =
                    ctor.ParameterList.Parameters
                        .Select(p => semanticModel.GetDeclaredSymbol(p))
                        .OfType<IParameterSymbol>()
                        .ToList();

                allParameters.AddRange(parameters);

                if (!ConstructorContainsOnlyValidAssignments(ctor, parameters, semanticModel))
                {
                    return; // reject class
                }
            }

            // Collect primary constructor parameters (C# 12)
            var primaryCtorParams = GetPrimaryConstructorParameters(classDecl, semanticModel);

            var allParams = allParameters.Concat(primaryCtorParams).ToList();

            if (allParams.Count == 0)
            {
                return;
            }

            foreach (var property in properties)
            {
                if (!IsPropertyAssignedDirectlyFromParameter(property, allParams, semanticModel))
                {
                    return; // Any violation → not a record candidate
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptor,
                classDecl.Identifier.GetLocation()));
        }

        private bool IsPropertyAssignedDirectlyFromParameter(
            PropertyDeclarationSyntax property,
            List<IParameterSymbol> parameters,
            SemanticModel model)
        {
            // 1. Expression-bodied: => name
            if (property.ExpressionBody != null)
            {
                var symbol = model.GetSymbolInfo(property.ExpressionBody.Expression).Symbol;
                return parameters.Contains(symbol, SymbolEqualityComparer.Default);
            }

            // 2. Initializer: { get; } = name;
            if (property.Initializer != null)
            {
                var symbol = model.GetSymbolInfo(property.Initializer.Value).Symbol;
                return parameters.Contains(symbol, SymbolEqualityComparer.Default);
            }

            // 3. Constructor assignment
            var classDecl = property.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            var ctors = classDecl.Members.OfType<ConstructorDeclarationSyntax>();

            foreach (var ctor in ctors)
            {
                var assignments = ctor.Body?
                    .DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>() ?? Enumerable.Empty<AssignmentExpressionSyntax>();

                foreach (var assignment in assignments)
                {
                    if (!IsPropertyAssignment(property, assignment, model))
                    {
                        continue;
                    }

                    // KEY: RHS must be IDENTIFIER ONLY (no transformations)
                    if (assignment.Right is IdentifierNameSyntax identifier)
                    {
                        var symbol = model.GetSymbolInfo(identifier).Symbol;
                        return parameters.Contains(symbol, SymbolEqualityComparer.Default);
                    }
                }

                if (ctor.ExpressionBody?.Expression is AssignmentExpressionSyntax expressionBodyAssignment
                    && IsPropertyAssignment(property, expressionBodyAssignment, model)
                    && expressionBodyAssignment.Right is IdentifierNameSyntax expressionBodyIdentifier)
                {
                    var symbol = model.GetSymbolInfo(expressionBodyIdentifier).Symbol;
                    return parameters.Contains(symbol, SymbolEqualityComparer.Default);
                }
            }

            return false;
        }

        private static bool IsPropertyAssignment(
            PropertyDeclarationSyntax property,
            AssignmentExpressionSyntax assignment,
            SemanticModel model)
        {
            var leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
            var propSymbol = model.GetDeclaredSymbol(property);

            return SymbolEqualityComparer.Default.Equals(leftSymbol, propSymbol);
        }

        private static List<IParameterSymbol> GetPrimaryConstructorParameters(
            ClassDeclarationSyntax classDecl,
            SemanticModel model)
        {
            var result = new List<IParameterSymbol>();

            var parameterListProp = typeof(ClassDeclarationSyntax).GetProperty("ParameterList");

            if (parameterListProp?.GetValue(classDecl) is ParameterListSyntax parameterList)
            {
                foreach (var param in parameterList.Parameters)
                {
                    var symbol = model.GetDeclaredSymbol(param);

                    if (symbol != null)
                    {
                        result.Add(symbol);
                    }
                }
            }

            return result;
        }

        private static bool IsReadOnlyProperty(PropertyDeclarationSyntax property)
        {
            // Expression-bodied property: => ...
            if (property.ExpressionBody != null)
            {
                return true;
            }

            if (property.AccessorList == null)
            {
                return false;
            }

            var accessors = property.AccessorList.Accessors;

            // Must have getter
            var getter = accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));

            if (getter == null)
            {
                return false;
            }

            // Must NOT have setter/init
            var hasSetter =
                accessors.Any(a =>
                    a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                    a.IsKind(SyntaxKind.InitAccessorDeclaration));

            return !hasSetter;
        }

        private static bool SupportsRecords(CSharpParseOptions parseOptions)
        {
            if (parseOptions == null)
            {
                return true;
            }

            var languageVersion = parseOptions.LanguageVersion;

            return languageVersion == LanguageVersion.Default || languageVersion >= LanguageVersion.CSharp9;
        }

        private static bool HasSupportedBaseType(INamedTypeSymbol classSymbol)
        {
            if (classSymbol == null)
            {
                return false;
            }

            var baseType = classSymbol.BaseType;

            return baseType == null
                || baseType.SpecialType == SpecialType.System_Object
                || baseType.IsRecord;
        }

        private static bool ConstructorContainsOnlyValidAssignments(
            ConstructorDeclarationSyntax ctor,
            List<IParameterSymbol> parameters,
            SemanticModel model)
        {
            if (ctor.Body != null)
            {
                foreach (var statement in ctor.Body.Statements)
                {
                    // Must be: Name = name;
                    if (statement is ExpressionStatementSyntax exprStmt &&
                        exprStmt.Expression is AssignmentExpressionSyntax assignment)
                    {
                        // LHS must be a property
                        var leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol as IPropertySymbol;
                        if (leftSymbol == null)
                        {
                            return false;
                        }

                        // RHS must be direct parameter (Identifier only)
                        if (!(assignment.Right is IdentifierNameSyntax identifier))
                        {
                            return false;
                        }

                        var rightSymbol = model.GetSymbolInfo(identifier).Symbol;

                        if (!parameters.Contains(rightSymbol, SymbolEqualityComparer.Default))
                        {
                            return false;
                        }

                        continue;
                    }

                    // Anything else = invalid (Console.Write, if, etc.)
                    return false;
                }
            }
            else if (ctor.ExpressionBody != null)
            {
                if (!(ctor.ExpressionBody.Expression is AssignmentExpressionSyntax assignment))
                {
                    return false;
                }

                var leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol as IPropertySymbol;
                if (leftSymbol == null)
                {
                    return false;
                }

                if (!(assignment.Right is IdentifierNameSyntax identifier))
                {
                    return false;
                }

                var rightSymbol = model.GetSymbolInfo(identifier).Symbol;

                if (!parameters.Contains(rightSymbol, SymbolEqualityComparer.Default))
                {
                    return false;
                }

                return true;
            }

            return true;
        }
    }
}
