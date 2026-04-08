using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis;

namespace Sellorio.Analyzers.CodeFixes.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseRecordInsteadOfImmutableClassCodeFixProvider)), Shared]
    public class UseRecordInsteadOfImmutableClassCodeFixProvider : CodeFixProviderBase
    {
        private const string _title = "Convert to record";

        internal override Expression<Func<DiagnosticDescriptorValues>> Descriptor => () => Descriptors.SE0011;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var classDeclaration = FindClassDeclaration(root, diagnostic.Location.SourceSpan);

            if (classDeclaration == null || !SupportsRecords(context.Document.Project.ParseOptions as CSharpParseOptions))
                return;

            context.RegisterCodeFix(
                CreateDocumentCodeAction(
                    title: _title,
                    createChangedDocument: ct => ConvertToRecordAsync(context.Document, diagnostic.Location.SourceSpan, ct),
                    equivalenceKey: _title),
                diagnostic);
        }

        private static ClassDeclarationSyntax FindClassDeclaration(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            return token.Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        }

        private static async Task<Document> ConvertToRecordAsync(
            Document document,
            TextSpan declarationSpan,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var classDeclaration = FindClassDeclaration(root, declarationSpan);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (classDeclaration == null || semanticModel == null)
                return document;

            var recordDeclaration = CreateRecordDeclaration(classDeclaration, semanticModel, document.Project.ParseOptions as CSharpParseOptions);
            if (recordDeclaration == null)
                return document;

            var newRoot = root.ReplaceNode(classDeclaration, recordDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        private static RecordDeclarationSyntax CreateRecordDeclaration(
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            CSharpParseOptions parseOptions)
        {
            return TryCreatePositionalRecordDeclaration(classDeclaration, semanticModel, parseOptions)
                ?? CreateNominalRecordDeclaration(classDeclaration, parseOptions);
        }

        private static RecordDeclarationSyntax TryCreatePositionalRecordDeclaration(
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            CSharpParseOptions parseOptions)
        {
            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();
            if (!CanConvertPropertiesToPrimaryConstructorParameters(properties))
                return null;

            var canonicalConstructor = FindCanonicalConstructor(classDeclaration, semanticModel, properties);
            if (canonicalConstructor == null)
                return null;

            var attributeText = classDeclaration.AttributeLists.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, classDeclaration.AttributeLists.Select(a => a.ToString())) + Environment.NewLine;
            var modifiersText = classDeclaration.Modifiers.Count == 0
                ? string.Empty
                : string.Join(" ", classDeclaration.Modifiers.Select(m => m.Text)) + " ";
            var typeParameterText = classDeclaration.TypeParameterList?.ToString() ?? string.Empty;
            var parameterText = string.Join(", ", properties.Select(CreatePrimaryConstructorParameterText));
            var baseListText = classDeclaration.BaseList == null ? string.Empty : " " + classDeclaration.BaseList;
            var constraintText = classDeclaration.ConstraintClauses.Count == 0
                ? string.Empty
                : Environment.NewLine + string.Join(Environment.NewLine, classDeclaration.ConstraintClauses.Select(c => "    " + c.ToString()));

            var remainingMembers = classDeclaration.Members
                .Where(member => !ReferenceEquals(member, canonicalConstructor))
                .Where(member => !(member is PropertyDeclarationSyntax property && properties.Contains(property)))
                .Select(member => TrimLeadingNewLines(member.ToFullString()).TrimEnd())
                .Where(memberText => memberText.Length > 0)
                .ToList();

            var recordDeclarationText = attributeText
                + modifiersText
                + "record "
                + classDeclaration.Identifier.ValueText
                + typeParameterText
                + "("
                + parameterText
                + ")"
                + baseListText
                + constraintText;

            if (remainingMembers.Count == 0)
            {
                recordDeclarationText += ";";
            }
            else
            {
                recordDeclarationText += Environment.NewLine
                    + "{"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, remainingMembers)
                    + Environment.NewLine
                    + "}";
            }

            return (SyntaxFactory.ParseMemberDeclaration(recordDeclarationText, 0, parseOptions, consumeFullText: true) as RecordDeclarationSyntax)
                ?.WithLeadingTrivia(classDeclaration.GetLeadingTrivia())
                .WithTrailingTrivia(classDeclaration.GetTrailingTrivia());
        }

        private static ConstructorDeclarationSyntax FindCanonicalConstructor(
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            List<PropertyDeclarationSyntax> properties)
        {
            var propertySymbols = properties
                .Select(property => semanticModel.GetDeclaredSymbol(property))
                .OfType<IPropertySymbol>()
                .ToList();

            return classDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .Where(ctor => ctor.ParameterList.Parameters.Count > 0)
                .Where(ctor => ctor.Initializer == null)
                .Select(ctor => new
                {
                    Constructor = ctor,
                    AssignedProperties = GetAssignedProperties(ctor, semanticModel)
                })
                .Where(x => x.AssignedProperties != null)
                .OrderByDescending(x => x.Constructor.ParameterList.Parameters.Count)
                .FirstOrDefault(x => propertySymbols.All(symbol => x.AssignedProperties.Contains(symbol)))
                ?.Constructor;
        }

        private static HashSet<IPropertySymbol> GetAssignedProperties(
            ConstructorDeclarationSyntax constructor,
            SemanticModel semanticModel)
        {
            var assignedProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

            if (constructor.Body != null)
            {
                foreach (var statement in constructor.Body.Statements)
                {
                    if (!(statement is ExpressionStatementSyntax expressionStatement)
                        || !(expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression)
                        || !TryGetAssignedProperty(assignmentExpression, semanticModel, out var propertySymbol))
                    {
                        return null;
                    }

                    assignedProperties.Add(propertySymbol);
                }
            }
            else if (constructor.ExpressionBody != null)
            {
                if (!(constructor.ExpressionBody.Expression is AssignmentExpressionSyntax assignmentExpression)
                    || !TryGetAssignedProperty(assignmentExpression, semanticModel, out var propertySymbol))
                {
                    return null;
                }

                assignedProperties.Add(propertySymbol);
            }

            return assignedProperties;
        }

        private static bool TryGetAssignedProperty(
            AssignmentExpressionSyntax assignmentExpression,
            SemanticModel semanticModel,
            out IPropertySymbol propertySymbol)
        {
            propertySymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol as IPropertySymbol;
            if (propertySymbol == null)
                return false;

            return assignmentExpression.Right is IdentifierNameSyntax identifier
                && semanticModel.GetSymbolInfo(identifier).Symbol is IParameterSymbol;
        }

        private static bool CanConvertPropertiesToPrimaryConstructorParameters(List<PropertyDeclarationSyntax> properties)
        {
            return properties.Count > 0
                && properties.All(property => property.Modifiers.Count == 0
                    || property.Modifiers.Count == 1 && property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        }

        private static string CreatePrimaryConstructorParameterText(PropertyDeclarationSyntax property)
        {
            var attributeText = string.Join(" ", property.AttributeLists.Select(CreatePrimaryConstructorAttributeText));
            if (attributeText.Length > 0)
            {
                attributeText += " ";
            }

            return attributeText + property.Type.WithoutTrivia().ToString() + " " + property.Identifier.ValueText;
        }

        private static string CreatePrimaryConstructorAttributeText(AttributeListSyntax attributeList)
        {
            var propertyTarget = attributeList.Target;
            if (propertyTarget == null || !propertyTarget.Identifier.IsKind(SyntaxKind.PropertyKeyword))
            {
                propertyTarget = SyntaxFactory.AttributeTargetSpecifier(
                    SyntaxFactory.Token(SyntaxKind.PropertyKeyword),
                    SyntaxFactory.Token(SyntaxKind.ColonToken));
            }

            return attributeList.WithTarget(propertyTarget).NormalizeWhitespace().ToFullString();
        }

        private static string TrimLeadingNewLines(string text)
        {
            var result = text;

            while (result.StartsWith("\r", StringComparison.Ordinal) || result.StartsWith("\n", StringComparison.Ordinal))
            {
                result = result.Substring(1);
            }

            return result;
        }

        private static RecordDeclarationSyntax CreateNominalRecordDeclaration(
            ClassDeclarationSyntax classDeclaration,
            CSharpParseOptions parseOptions)
        {
            var declarationText = classDeclaration.ToFullString();
            var relativeKeywordStart = classDeclaration.Keyword.SpanStart - classDeclaration.FullSpan.Start;
            var recordDeclarationText = declarationText.Remove(relativeKeywordStart, classDeclaration.Keyword.Span.Length)
                .Insert(relativeKeywordStart, SyntaxFacts.GetText(SyntaxKind.RecordKeyword));

            return (SyntaxFactory.ParseMemberDeclaration(recordDeclarationText, 0, parseOptions, consumeFullText: true) as RecordDeclarationSyntax)
                ?.WithLeadingTrivia(classDeclaration.GetLeadingTrivia())
                .WithTrailingTrivia(classDeclaration.GetTrailingTrivia());
        }

        private static bool SupportsRecords(CSharpParseOptions parseOptions)
        {
            if (parseOptions == null)
                return true;

            var languageVersion = parseOptions.LanguageVersion;

            return languageVersion == LanguageVersion.Default || languageVersion >= LanguageVersion.CSharp9;
        }
    }
}
