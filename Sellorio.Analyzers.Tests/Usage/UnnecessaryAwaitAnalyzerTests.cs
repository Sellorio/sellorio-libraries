using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Usage.UnnecessaryAwaitAnalyzer,
    Sellorio.Analyzers.CodeFixes.Usage.UnnecessaryAwaitCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Usage;

public class UnnecessaryAwaitAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenAwaitingTaskFromResultInReturnStatement()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        await Task.Delay(1);
return {|SE0032:await Task.FromResult(42)|};
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenAwaitingCompletedTaskStatement()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(1);
{|SE0032:await Task.CompletedTask|};
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_ReplacesAwaitTaskFromResultInReturnStatement()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        await Task.Delay(1);
return {|SE0032:await Task.FromResult(42)|};
    }
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        await Task.Delay(1);
        return 42;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesAwaitTaskFromResultInVariableInitializer()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
var value = {|SE0032:await Task.FromResult(42)|};
        await Task.Delay(1);
        return value;
    }
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<int> GetValueAsync()
    {
        var value = 42;
        await Task.Delay(1);
        return value;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RemovesAwaitCompletedTaskStatement()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(1);
{|SE0032:await Task.CompletedTask|};
    }
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(1);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesAwaitTaskFromResultStatement_WhenReplacementIsAValidStatementExpression()
    {
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(1);
{|SE0032:await Task.FromResult(Log())|};
    }

    private int Log()
    {
        return 42;
    }
}";
        var fixedSource = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(1);
        Log();
    }

    private int Log()
    {
        return 42;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
