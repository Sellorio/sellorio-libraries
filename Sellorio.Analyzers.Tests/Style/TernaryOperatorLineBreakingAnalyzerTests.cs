using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.TernaryOperatorLineBreakingAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.TernaryOperatorLineBreakingCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class TernaryOperatorLineBreakingAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenQuestionAndColonAreOnWrongLines()
    {
        var source = @"
public class TestClass
{
    public string GetText(bool condition, string first, string second)
    {
return {|SE0002:condition ?
            first :
            second|};
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_MovesQuestionAndColonToIndentedLines()
    {
        var source = @"
public class TestClass
{
    public string GetText(bool condition, string first, string second)
    {
return {|SE0002:condition ?
            first :
            second|};
    }
}";
        var fixedSource = @"
public class TestClass
{
    public string GetText(bool condition, string first, string second)
    {
        return condition
            ? first
            : second;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_FixesColonWhenQuestionIsAlreadyOnNextLine()
    {
        var source = @"
public class TestClass
{
    public string GetText(bool condition, string first, string second)
    {
return {|SE0002:condition
            ? first :
            second|};
    }
}";
        var fixedSource = @"
public class TestClass
{
    public string GetText(bool condition, string first, string second)
    {
        return condition
            ? first
            : second;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenQuestionAndColonAlreadyUseExpectedLineBreaks()
    {
        var source = @"
public class TestClass
{
    public string GetText(bool condition, string first, string second)
    {
        return condition
            ? first
            : second;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenIfStatementContainsQuestionAndColonTokens()
    {
        var source = @"
public class TestClass
{
    public int GetValue(bool condition)
    {
        if (condition)
        {
            int? value = null;

            switch (value)
            {
                case null:
                    return 0;
            }
        }

        return 1;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenNestedTernariesAlreadyUseExpectedLineBreaks()
    {
        var source = @"
public class TestClass
{
    public string GetText(bool outerCondition, bool innerCondition, string first, string second, string third)
    {
        return outerCondition
            ? innerCondition
                ? first
                : second
            : third;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
