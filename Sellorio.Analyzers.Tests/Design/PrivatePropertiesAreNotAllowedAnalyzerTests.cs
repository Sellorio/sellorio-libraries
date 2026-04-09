using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Sellorio.Analyzers.CodeAnalysis.Design;
using Sellorio.Analyzers.CodeFixes.Design;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Design.PrivatePropertiesAreNotAllowedAnalyzer,
    Sellorio.Analyzers.CodeFixes.Design.PrivatePropertiesAreNotAllowedCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Design;

public class PrivatePropertiesAreNotAllowedAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_PublicProperty()
    {
        var source = @"
public class TestClass
{
    public int Value { get; set; }
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_PrivateAutoProperty()
    {
        var source = @"
public class TestClass
{
    private int {|SE0003:Value|} { get; set; }
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_ConvertsAutoPropertyToField()
    {
        var source = @"
public class TestClass
{
    private int {|SE0003:Value|} { get; set; }

    public void SetValue(int value)
    {
        Value = value;
    }
}";
        var fixedSource = @"
public class TestClass
{
    private int Value;

    public void SetValue(int value)
    {
        Value = value;
    }
}";
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ConvertsGetterOnlyAutoPropertyToReadonlyField()
    {
        var source = @"
public class TestClass
{
    private string {|SE0003:Value|} { get; } = ""value"";

    public string GetValue()
    {
        return Value;
    }
}";
        var fixedSource = @"
public class TestClass
{
    private readonly string Value = ""value"";

    public string GetValue()
    {
        return Value;
    }
}";
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ConvertsExpressionBodiedPropertyToReadonlyField()
    {
        var source = @"
public class TestClass
{
    private int {|SE0003:Value|} => 42;

    public int GetValue()
    {
        return Value;
    }
}";
        var fixedSource = @"
public class TestClass
{
    private readonly int Value = 42;

    public int GetValue()
    {
        return Value;
    }
}";
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoCodeFix_NonAutoProperty()
    {
        var source = @"
public class TestClass
{
    private int Value
    {
        get { return 42; }
    }
}";

        Assert.Equal(0, await GetCodeFixCountAsync(source));
    }

    private static async Task<int> GetCodeFixCountAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview))
            .WithProjectMetadataReferences(projectId, GetMetadataReferences())
            .AddDocument(documentId, "TestClass.cs", SourceText.From(source));
        var document = solution.GetDocument(documentId);
        var compilation = await document.Project.GetCompilationAsync();
        var analyzer = new PrivatePropertiesAreNotAllowedAnalyzer();
        var diagnostics = await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
        var diagnostic = Assert.Single(diagnostics);

        var actions = new List<CodeAction>();
        var codeFixProvider = new PrivatePropertiesAreNotAllowedCodeFixProvider();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context);

        return actions.Count;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
