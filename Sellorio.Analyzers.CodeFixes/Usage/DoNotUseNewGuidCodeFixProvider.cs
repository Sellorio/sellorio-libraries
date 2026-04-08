using System;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Sellorio.Analyzers.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeFixes;

namespace Sellorio.Analyzers.CodeFixes.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DoNotUseNewGuidCodeFixProvider)), Shared]
    public class DoNotUseNewGuidCodeFixProvider : CodeFixProviderBase
    {
        private const string UseDefaultEquivalenceKey = "UseDefault";
        private const string UseNewGuidEquivalenceKey = "UseNewGuid";
        private const string UseDefaultTitle = "Use default";
        private const string UseNewGuidTitle = "Use Guid.NewGuid()";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0031;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null || semanticModel == null)
                return;

            var diagnostic = context.Diagnostics[0];
            var objectCreation = FindObjectCreation(root, diagnostic.Location.SourceSpan);
            if (TryCreateReplacement(objectCreation, semanticModel, context.Document.Project.ParseOptions as CSharpParseOptions, ReplacementKind.Default, out _, out var defaultTitle))
            {
                context.RegisterCodeFix(
                    CreateDocumentCodeAction(
                        title: defaultTitle,
                        createChangedDocument: ct => ApplyFixAsync(context.Document, diagnostic.Location.SourceSpan, ReplacementKind.Default, ct),
                        equivalenceKey: UseDefaultEquivalenceKey),
                    diagnostic);
            }

            if (TryCreateReplacement(objectCreation, semanticModel, context.Document.Project.ParseOptions as CSharpParseOptions, ReplacementKind.NewGuid, out _, out _))
            {
                context.RegisterCodeFix(
                    CreateDocumentCodeAction(
                        title: UseNewGuidTitle,
                        createChangedDocument: ct => ApplyFixAsync(context.Document, diagnostic.Location.SourceSpan, ReplacementKind.NewGuid, ct),
                        equivalenceKey: UseNewGuidEquivalenceKey),
                    diagnostic);
            }
        }

        private static BaseObjectCreationExpressionSyntax FindObjectCreation(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?
                .AncestorsAndSelf()
                .OfType<BaseObjectCreationExpressionSyntax>()
                .FirstOrDefault();
        }

        private static async Task<Document> ApplyFixAsync(
            Document document,
            TextSpan diagnosticSpan,
            ReplacementKind replacementKind,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root == null || semanticModel == null)
                return document;

            var objectCreation = FindObjectCreation(root, diagnosticSpan);
            if (!TryCreateReplacement(objectCreation, semanticModel, document.Project.ParseOptions as CSharpParseOptions, replacementKind, out var replacement, out _))
                return document;

            var updatedExpression = replacement
                .WithLeadingTrivia(objectCreation.GetLeadingTrivia())
                .WithTrailingTrivia(objectCreation.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var updatedRoot = root.ReplaceNode(objectCreation, updatedExpression);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static bool TryCreateReplacement(
            BaseObjectCreationExpressionSyntax objectCreation,
            SemanticModel semanticModel,
            CSharpParseOptions parseOptions,
            ReplacementKind replacementKind,
            out ExpressionSyntax replacement,
            out string title)
        {
            replacement = null;
            title = null;
            if (objectCreation == null)
                return false;

            var typeSymbol = semanticModel.GetTypeInfo(objectCreation).Type;
            if (!IsGuid(typeSymbol))
                return false;

            var typeName = typeSymbol.ToMinimalDisplayString(semanticModel, objectCreation.SpanStart);
            switch (replacementKind)
            {
                case ReplacementKind.Default:
                    if (CanUseDefaultLiteral(objectCreation, semanticModel, parseOptions, typeSymbol))
                    {
                        replacement = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
                        title = UseDefaultTitle;
                    }
                    else
                    {
                        replacement = SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(typeName));
                        title = $"Use default({typeName})";
                    }

                    return true;

                case ReplacementKind.NewGuid:
                    replacement = SyntaxFactory.ParseExpression(typeName + ".NewGuid()");
                    title = UseNewGuidTitle;
                    return true;

                default:
                    return false;
            }
        }

        private static bool CanUseDefaultLiteral(
            BaseObjectCreationExpressionSyntax objectCreation,
            SemanticModel semanticModel,
            CSharpParseOptions parseOptions,
            ITypeSymbol typeSymbol)
        {
            if (parseOptions == null || parseOptions.LanguageVersion < LanguageVersion.CSharp7_1)
                return false;

            if (objectCreation is ImplicitObjectCreationExpressionSyntax || HasExplicitTargetType(objectCreation, semanticModel))
                return true;

            var defaultLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
            var typeInfo = semanticModel.GetSpeculativeTypeInfo(
                objectCreation.SpanStart,
                defaultLiteral,
                SpeculativeBindingOption.BindAsExpression);

            return SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeSymbol)
                || SymbolEqualityComparer.Default.Equals(typeInfo.ConvertedType, typeSymbol);
        }

        private static bool HasExplicitTargetType(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            switch (expression.Parent)
            {
                case EqualsValueClauseSyntax equalsValueClause:
                    return HasExplicitTargetType(equalsValueClause);

                case AssignmentExpressionSyntax assignmentExpression when assignmentExpression.Right == expression:
                    return semanticModel.GetTypeInfo(assignmentExpression.Left).Type != null;

                case ReturnStatementSyntax _:
                    return GetEnclosingReturnType(expression) != null;

                case ArrowExpressionClauseSyntax arrowExpressionClause when arrowExpressionClause.Expression == expression:
                    return GetArrowExpressionReturnType(arrowExpressionClause) != null;

                default:
                    return false;
            }
        }

        private static bool HasExplicitTargetType(EqualsValueClauseSyntax equalsValueClause)
        {
            switch (equalsValueClause.Parent)
            {
                case VariableDeclaratorSyntax variableDeclarator when variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration:
                    return !variableDeclaration.Type.IsVar;

                case PropertyDeclarationSyntax _:
                case FieldDeclarationSyntax _:
                    return true;

                default:
                    return false;
            }
        }

        private static TypeSyntax GetEnclosingReturnType(SyntaxNode expression)
        {
            var returnStatement = expression.Parent as ReturnStatementSyntax;
            if (returnStatement == null)
                return null;

            var member = returnStatement.Parent?.Parent;
            switch (member)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    return methodDeclaration.ReturnType;

                case LocalFunctionStatementSyntax localFunctionStatement:
                    return localFunctionStatement.ReturnType;

                case ConversionOperatorDeclarationSyntax conversionOperatorDeclaration:
                    return conversionOperatorDeclaration.Type;

                case OperatorDeclarationSyntax operatorDeclaration:
                    return operatorDeclaration.ReturnType;

                default:
                    return null;
            }
        }

        private static TypeSyntax GetArrowExpressionReturnType(ArrowExpressionClauseSyntax arrowExpressionClause)
        {
            switch (arrowExpressionClause.Parent)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    return methodDeclaration.ReturnType;

                case PropertyDeclarationSyntax propertyDeclaration:
                    return propertyDeclaration.Type;

                case IndexerDeclarationSyntax indexerDeclaration:
                    return indexerDeclaration.Type;

                case LocalFunctionStatementSyntax localFunctionStatement:
                    return localFunctionStatement.ReturnType;

                case OperatorDeclarationSyntax operatorDeclaration:
                    return operatorDeclaration.ReturnType;

                case ConversionOperatorDeclarationSyntax conversionOperatorDeclaration:
                    return conversionOperatorDeclaration.Type;

                default:
                    return null;
            }
        }

        private static bool IsGuid(ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.Name == nameof(Guid)
                && namedTypeSymbol.ContainingNamespace?.ToDisplayString() == "System";
        }

        private enum ReplacementKind
        {
            Default,
            NewGuid,
        }
    }
}
