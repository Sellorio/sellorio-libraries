using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.ExtraWhitespaceBeforeCloseBraceAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.ExtraWhitespaceBeforeCloseBraceCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class ExtraWhitespaceBeforeCloseBraceAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenBlankLineImmediatelyPrecedesCloseBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        var value = 1;

{|SE0013:}|}
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RemovesSingleBlankLineBeforeCloseBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        var value = 1;

{|SE0013:}|}
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        var value = 1;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RemovesEveryBlankLineBeforeCloseBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        var value = 1;



{|SE0013:}|}
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        var value = 1;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RemovesBlankLinesAfterComment()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        // keep comment


{|SE0013:}|}
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // keep comment
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RemovesBlankLinesInEmptyBlock()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {


{|SE0013:}|}
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenCommentImmediatelyPrecedesCloseBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        // keep comment
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
