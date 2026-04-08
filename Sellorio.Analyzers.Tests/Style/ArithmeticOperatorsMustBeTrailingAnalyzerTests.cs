using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.ArithmeticOperatorsMustBeTrailingAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.ArithmeticOperatorsMustBeTrailingCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class ArithmeticOperatorsMustBeTrailingAnalyzerTests
{
    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    public async Task Diagnostic_WhenArithmeticOperatorStartsOnNewLine(string op)
    {
        var source = $@"
public class TestClass
{{
    public int Calculate(int a, int b)
    {{
        return a
            {{|SE0009:{op}|}} b;
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    public async Task CodeFix_MovesArithmeticOperatorToPreviousLine(string op)
    {
        var source = $@"
public class TestClass
{{
    public int Calculate(int a, int b)
    {{
        return a
            {{|SE0009:{op}|}} b;
    }}
}}";
        var fixedSource = $@"
public class TestClass
{{
    public int Calculate(int a, int b)
    {{
        return a {op}
            b;
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_PreservesTrailingLineComment()
    {
        var source = @"
public class TestClass
{
    public int Calculate(int a, int b)
    {
        return a // keep this comment
            {|SE0009:+|} b;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public int Calculate(int a, int b)
    {
        return a + // keep this comment
            b;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RemovesNowEmptyOperatorLine()
    {
        var source = @"
public class TestClass
{
    public int Calculate(int a, int b)
    {
        return a
            {|SE0009:+|}
            b;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public int Calculate(int a, int b)
    {
        return a +
            b;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenArithmeticOperatorIsTrailing()
    {
        var source = @"
public class TestClass
{
    public int Calculate(int a, int b)
    {
        return a +
            b;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
