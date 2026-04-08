using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.CloseParenthesisMustNotBeFirstOnLineAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.CloseParenthesisMustNotBeFirstOnLineCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class CloseParenthesisMustNotBeFirstOnLineAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenCloseParenthesisStartsOnNewLine()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2
{|SE0033:)|};
    }

    private void Execute(int first, int second)
    {
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_MovesCloseParenthesisAndRemainingCodeToPreviousLine()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2
{|SE0033:)|};
    }

    private void Execute(int first, int second)
    {
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2);
    }

    private void Execute(int first, int second)
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_PreservesTrailingLineCommentOnPreviousLine()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2 // keep this comment
{|SE0033:)|};
    }

    private void Execute(int first, int second)
    {
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2); // keep this comment
    }

    private void Execute(int first, int second)
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_PreservesTrailingLineCommentOnCloseParenthesisLine()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2
{|SE0033:)|}; // keep this comment
    }

    private void Execute(int first, int second)
    {
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2); // keep this comment
    }

    private void Execute(int first, int second)
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_MergesTrailingLineComments()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2 // first comment
{|SE0033:)|}; // second comment
    }

    private void Execute(int first, int second)
    {
    }
}";
        var fixedSource = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2); // first comment. second comment
    }

    private void Execute(int first, int second)
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenCloseParenthesisIsNotFirstOnLine()
    {
        var source = @"
public class TestClass
{
    public void DoWork()
    {
        Execute(
            1,
            2);
    }

    private void Execute(int first, int second)
    {
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
