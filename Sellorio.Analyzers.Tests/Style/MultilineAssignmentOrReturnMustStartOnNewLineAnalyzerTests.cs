using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.MultilineAssignmentOrReturnMustStartOnNewLineAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.MultilineAssignmentOrReturnMustStartOnNewLineCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class MultilineAssignmentOrReturnMustStartOnNewLineAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenMultilineReturnExpressionStartsOnSameLine()
    {
        var source = @"
public class TestClass
{
    public int GetValue(int first, int second)
    {
        return {|SE0021:Combine(
            first,
            second)|};
    }

    private static int Combine(int first, int second) => first + second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenMultilineAssignmentExpressionStartsOnSameLine()
    {
        var source = @"
public class TestClass
{
    public void DoWork(int first, int second)
    {
        var value = {|SE0021:Combine(
            first,
            second)|};
    }

    private static int Combine(int first, int second) => first + second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_MovesReturnExpressionToNextLine()
    {
        var source = @"
public class TestClass
{
    public int GetValue(int first, int second)
    {
        return {|SE0021:Combine(
            first,
            second)|};
    }

    private static int Combine(int first, int second) => first + second;
}";
        var fixedSource = @"
public class TestClass
{
    public int GetValue(int first, int second)
    {
        return
            Combine(
                first,
                second);
    }

    private static int Combine(int first, int second) => first + second;
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReindentsMemberAccessChainInAssignment()
    {
        var source = @"
public class TestClass
{
    private ServiceWrapper Service { get; } = new ServiceWrapper();

    public void DoWork()
    {
        var value = {|SE0021:Service
            .GetValue(
                1,
                2)|};
    }

    private sealed class ServiceWrapper
    {
        public int GetValue(int first, int second) => first + second;
    }
}";
        var fixedSource = @"
public class TestClass
{
    private ServiceWrapper Service { get; } = new ServiceWrapper();

    public void DoWork()
    {
        var value =
            Service
                .GetValue(
                    1,
                    2);
    }

    private sealed class ServiceWrapper
    {
        public int GetValue(int first, int second) => first + second;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReindentsSwitchExpressionWhenGoverningExpressionSpansMultipleLines()
    {
        var source = @"
public class TestClass
{
    private ServiceWrapper Service { get; } = new ServiceWrapper();

    public int GetValue()
    {
        var value = {|SE0021:Service
            .GetValue(
                1,
                2) switch
        {
            > 0 => 1,
            _ => 0,
        }|};

        return value;
    }

    private sealed class ServiceWrapper
    {
        public int GetValue(int first, int second) => first + second;
    }
}";
        var fixedSource = @"
public class TestClass
{
    private ServiceWrapper Service { get; } = new ServiceWrapper();

    public int GetValue()
    {
        var value =
            Service
                .GetValue(
                    1,
                    2) switch
            {
                > 0 => 1,
                _ => 0,
            };

        return value;
    }

    private sealed class ServiceWrapper
    {
        public int GetValue(int first, int second) => first + second;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenSwitchExpressionGoverningExpressionFitsOnSingleLine()
    {
        var source = @"
public class TestClass
{
    public int GetValue(int number)
    {
        return number switch
        {
            1 => 2,
            _ => 3,
        };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenObjectInitializerFollowsSingleLineConstruction()
    {
        var source = @"
public class TestClass
{
    public object Create(int first, int second)
    {
        return new TestObject(first, second)
        {
            Total = first + second,
        };
    }

    private sealed class TestObject
    {
        public TestObject(int first, int second)
        {
        }

        public int Total { get; set; }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenObjectInitializerFollowsMultilineConstruction()
    {
        var source = @"
public class TestClass
{
    public object Create(int first, int second)
    {
        return {|SE0021:new TestObject(
            first,
            second)
        {
            Total = first + second,
        }|};
    }

    private sealed class TestObject
    {
        public TestObject(int first, int second)
        {
        }

        public int Total { get; set; }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenLambdaBlockStartsAfterArrow()
    {
        var source = @"
using System;

public class TestClass
{
    public Func<int, int> Create()
    {
        return value =>
        {
            return value + 1;
        };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenWithExpressionFollowsSingleLineTarget()
    {
        var source = @"
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}

public record TestRecord(int Value, int OtherValue);

public class TestClass
{
    public TestRecord Update(TestRecord value)
    {
        return value with
        {
            OtherValue = value.Value + 1,
        };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenMultilineReturnExpressionAlreadyStartsOnNewLine()
    {
        var source = @"
public class TestClass
{
    public int GetValue(int first, int second)
    {
        return
            Combine(
                first,
                second);
    }

    private static int Combine(int first, int second) => first + second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenMultilineObjectInitializerIsntTheStartOfTheExpression()
    {
        var source = @"
public class TestClass
{
    public int First { get; set; }
    public int Second { get; set; }

    public TestClass(int first, int second)
    {
        First = first;
        Second = second;
    }

    public static int Combine(int first, int second)
    {
        return {|SE0021:Combine(new TestClass(first, second)
        {
            First = first,
            Second = second
        })|};
    }

    private static int Combine(TestClass input) => input.First + input.Second;
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
