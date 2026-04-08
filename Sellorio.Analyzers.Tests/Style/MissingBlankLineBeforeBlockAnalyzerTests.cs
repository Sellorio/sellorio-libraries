using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.MissingBlankLineBeforeBlockAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.MissingBlankLineBeforeBlockCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class MissingBlankLineBeforeBlockAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenIfBlockHasNoBlankLineBefore()
    {
        var source = @"
public class TestClass
{
    public void DoWork(int value)
    {
        value++;
        {|SE0015:if|} (value > 0)
        {
            value++;
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_AddsBlankLineBeforeIfBlock()
    {
        var source = @"
public class TestClass
{
    public void DoWork(int value)
    {
        value++;
        {|SE0015:if|} (value > 0)
        {
            value++;
        }
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork(int value)
    {
        value++;

        if (value > 0)
        {
            value++;
        }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_AddsBlankLineBeforeLeadingComment()
    {
        var source = @"
public class TestClass
{
    public void DoWork(int value)
    {
        value++;
        // keep comment with block
        {|SE0015:if|} (value > 0)
        {
            value++;
        }
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork(int value)
    {
        value++;

        // keep comment with block
        if (value > 0)
        {
            value++;
        }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenBlankLinePrecedesCommentedBlock()
    {
        var source = @"
public class TestClass
{
    public void DoWork(int value)
    {
        value++;

        // keep comment with block
        if (value > 0)
        {
            value++;
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenBlockImmediatelyFollowsOpenBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork(int value)
    {
        if (value > 0)
        {
            value++;
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
