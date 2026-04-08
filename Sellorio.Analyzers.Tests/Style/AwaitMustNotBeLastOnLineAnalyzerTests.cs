using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Style.AwaitMustNotBeLastOnLineAnalyzer,
    Sellorio.Analyzers.CodeFixes.Style.AwaitMustNotBeLastOnLineCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Style;

public class AwaitMustNotBeLastOnLineAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenAwaitIsLastOnLineInReturnStatement()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
return {|SE0030:await|}
            GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_MovesAwaitedInvocationToAwaitLine()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
return {|SE0030:await|}
            GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        return await GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReindentsRemainingLinesInReturnStatement()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync(int first, int second)
    {
return {|SE0030:await|}
            GetAsync(
                first,
                second);
    }

    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync(int first, int second)
    {
        return await GetAsync(
            first,
            second);
    }

    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReindentsRemainingLinesInIfCondition()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<bool> GetValueAsync(int value)
    {
if ({|SE0030:await|}
            GetAsync(
                value,
                value + 1))
        {
            return true;
        }

        return false;
    }

    private Task<bool> GetAsync(int first, int second) => Task.FromResult(first == second);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<bool> GetValueAsync(int value)
    {
        if (await GetAsync(
            value,
            value + 1))
        {
            return true;
        }

        return false;
    }

    private Task<bool> GetAsync(int first, int second) => Task.FromResult(first == second);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReindentsRemainingLinesWhenAwaitIsInsideArgumentList()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync(int first, int second)
    {
        return Combine(
            {|SE0030:await|}
                GetAsync(
                    first,
                    second),
            3);
    }

    private static int Combine(int first, int second) => first + second;
    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync(int first, int second)
    {
        return Combine(
            await GetAsync(
                first,
                second),
            3);
    }

    private static int Combine(int first, int second) => first + second;
    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReindentsMemberAccessChains()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    private ServiceWrapper Service { get; } = new ServiceWrapper();

    public async Task<int> GetValueAsync()
    {
return {|SE0030:await|}
            Service
                .GetAsync(
                    1,
                    2);
    }

    private sealed class ServiceWrapper
    {
        public Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
    }
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    private ServiceWrapper Service { get; } = new ServiceWrapper();

    public async Task<int> GetValueAsync()
    {
        return await Service
            .GetAsync(
                1,
                2);
    }

    private sealed class ServiceWrapper
    {
        public Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SupportsExpressionBodiedMembers()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
public async Task<int> GetValueAsync() => {|SE0030:await|}
        GetAsync(
            1,
            2);

    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync() => await GetAsync(
        1,
        2);

    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SupportsAsyncLambdaExpressionBodies()
    {
        var source = @"
using System;
using System.Threading.Tasks;

public class TestClass
{
    public Func<Task<int>> GetFactory()
    {
return async () => {|SE0030:await|}
            GetAsync(
                1,
                2);
    }

    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";
        var fixedSource = @"
using System;
using System.Threading.Tasks;

public class TestClass
{
    public Func<Task<int>> GetFactory()
    {
        return async () => await GetAsync(
            1,
            2);
    }

    private Task<int> GetAsync(int first, int second) => Task.FromResult(first + second);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_PreservesMultilineCommentBetweenAwaitAndExpression()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
return {|SE0030:await|}
            /* keep */
            GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        return await /* keep */ GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_PreservesMultilineCommentOnAwaitLine()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
return {|SE0030:await|} /* keep */
            GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        return await /* keep */ GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_WhenAwaitedExpressionStartsOnSameLine()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        return await GetAsync();
    }

    private Task<int> GetAsync() => Task.FromResult(1);
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
