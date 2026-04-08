using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.MultilineIfElseMissingLineBreakAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.MultilineIfElseMissingLineBreakCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class MultilineIfElseMissingLineBreakAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenElseIfConditionStartsOnSameLine()
    {
        var source = @"
public class TestClass
{
    public bool ShouldDoWork(int first, int second)
    {
        if (first < 0)
        {
            return false;
        }
        else if ({|SE0014:ShouldContinue(
            first,
            second)|})
        {
            return true;
        }

        return false;
    }

    private static bool ShouldContinue(int first, int second) => first < second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_MovesElseIfConditionToNextLine()
    {
        var source = @"
public class TestClass
{
    public bool ShouldDoWork(int first, int second)
    {
        if (first < 0)
        {
            return false;
        }
        else if ({|SE0014:ShouldContinue(
            first,
            second)|})
        {
            return true;
        }

        return false;
    }

    private static bool ShouldContinue(int first, int second) => first < second;
}";
        var fixedSource = @"
public class TestClass
{
    public bool ShouldDoWork(int first, int second)
    {
        if (first < 0)
        {
            return false;
        }
        else if (
            ShouldContinue(
                first,
                second))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldContinue(int first, int second) => first < second;
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenStandaloneIfConditionStartsOnSameLine()
    {
        var source = @"
public class TestClass
{
    public bool ShouldDoWork(int first, int second)
    {
        if (ShouldContinue(
            first,
            second))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldContinue(int first, int second) => first < second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenElseIfConditionAlreadyStartsOnNewLine()
    {
        var source = @"
public class TestClass
{
    public bool ShouldDoWork(int first, int second)
    {
        if (first < 0)
        {
            return false;
        }
        else if (
            ShouldContinue(
                first,
                second))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldContinue(int first, int second) => first < second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
