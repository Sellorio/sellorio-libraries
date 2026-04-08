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
}
