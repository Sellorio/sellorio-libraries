using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.ExtraWhitespaceAfterOpenBraceAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.ExtraWhitespaceAfterOpenBraceCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class ExtraWhitespaceAfterOpenBraceAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenBlankLineImmediatelyFollowsOpenBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
{|SE0012:{|}

        var value = 1;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RemovesSingleBlankLineBeforeCode()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
{|SE0012:{|}

        var value = 1;
    }
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
    public async Task CodeFix_RemovesEveryBlankLineBeforeCode()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
{|SE0012:{|}



        var value = 1;
    }
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
    public async Task CodeFix_RemovesBlankLinesBeforeComment()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
{|SE0012:{|}


        // keep comment
        var value = 1;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        // keep comment
        var value = 1;
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
{|SE0012:{|}


    }
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
    public async Task NoDiagnostic_WhenCommentImmediatelyFollowsOpenBrace()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        // keep comment
        var value = 1;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
