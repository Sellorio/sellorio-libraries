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
using Sellorio.Analyzers.CodeAnalysis.Style;
using Sellorio.Analyzers.CodeFixes.Style;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.DoNotUseMultilineCommentsAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.DoNotUseMultilineCommentsCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class DoNotUseMultilineCommentsAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenMultilineCommentIsUsed()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        {|SE0025:/* my comment */|}
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_ReplacesSingleLineBlockCommentWithSingleLineComment()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        {|SE0025:/* my comment */|}
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // my comment
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesMultilineBlockCommentWithSingleLineComments()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        {|SE0025:/* first comment
        second comment */|}
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // first comment
        // second comment
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesMultilineBlockCommentWithAsterisksWithSingleLineComments()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        {|SE0025:/* first comment
         * second comment */|}
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // first comment
        // second comment
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesMultilineBlockCommentWithSpaceWithSingleLineComments()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        {|SE0025:/*
        first comment
        second comment
        */|}
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // first comment
        // second comment
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesMultilineBlockCommentWithSpaceAndAsterisksWithSingleLineComments()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        {|SE0025:/*
         * first comment
         * second comment
         */|}
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // first comment
        // second comment
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_LeavesClosingLineAsBlockComment_WhenCodeFollowsClosingDelimiter()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        var y = 1;
        var x = {|SE0025:/* my comment
            continues */|} y;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        var y = 1;
        var x = // my comment
            /* continues */ y;
    }
}";

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };
        test.FixedState.ExpectedDiagnostics.Add(
            VerifyCS.Diagnostic("SE0025").WithSpan(8, 13, 8, 28));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CodeFix_IsNotRegistered_WhenSingleLineBlockCommentHasTrailingCode()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        var x = {|SE0025:/* my comment */|} y;
    }
}";

        var actions = await GetCodeActionsAsync(source);

        Assert.Empty(actions);
    }

    [Fact]
    public async Task NoDiagnostic_WhenSingleLineCommentsAreUsed()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        // my comment
        // another comment
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    private static async Task<IReadOnlyList<CodeAction>> GetCodeActionsAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, GetMetadataReferences())
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId);
        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var diagnostics = await compilation
            .WithAnalyzers([new DoNotUseMultilineCommentsAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);

        var actions = new List<CodeAction>();
        var provider = new DoNotUseMultilineCommentsCodeFixProvider();
        var context = new CodeFixContext(document, diagnostics[0], (action, _) => actions.Add(action), CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)
            ?.Split(Path.PathSeparator)
            ?? [];

        return trustedPlatformAssemblies.Select(path => MetadataReference.CreateFromFile(path));
    }
}
