using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Naming.ConfigureOptionsLambdaParameterNameAnalyzer,
    Sellorio.Analyzers.CodeFixes.Naming.ConfigureOptionsLambdaParameterNameCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Naming;

public class ConfigureOptionsLambdaParameterNameAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_LambdaParameterNamedO()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure(o => Console.WriteLine(o));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_LambdaParameterNamedOptions()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure(options => Console.WriteLine(options));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_SimpleLambdaParameterHasUnexpectedName()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
TestOptions.Configure({|SE0028:value|} => Console.WriteLine(value));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_NamedArgumentWithOptionsParameterHasUnexpectedName()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> callback, Action<int> optionsAction)
    {
    }
}

public class TestClass
{
    public void M()
    {
TestOptions.Configure(callback: x => Console.WriteLine(x), optionsAction: {|SE0028:value|} => Console.WriteLine(value));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_ParameterNameDoesNotContainConfigureOrOptions()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> callback)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure(value => Console.WriteLine(value));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_DelegateParameterHasMoreThanOneParameter()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int, int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure((left, right) => Console.WriteLine(left + right));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_ArgumentIsNotLambda()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        Action<int> configure = value => Console.WriteLine(value);
        TestOptions.Configure(configure);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_OverloadWithoutMatchingDelegateParameter()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(int configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure(42);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RenamesSimpleLambdaParameterAndReferences()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
TestOptions.Configure({|SE0028:value|} => Console.WriteLine(value + value));
    }
}";
        var fixedSource = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure(o => Console.WriteLine(o + o));
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesLambdaParameterForNamedArgument()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> callback, Action<int> optionsAction)
    {
    }
}

public class TestClass
{
    public void M()
    {
TestOptions.Configure(callback: x => Console.WriteLine(x), optionsAction: {|SE0028:value|} => Console.WriteLine(value));
    }
}";
        var fixedSource = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> callback, Action<int> optionsAction)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure(callback: x => Console.WriteLine(x), optionsAction: o => Console.WriteLine(o));
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesParenthesizedLambdaParameterAndReferences()
    {
        var source = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
TestOptions.Configure((int {|SE0028:value|}) => Console.WriteLine(value));
    }
}";
        var fixedSource = @"
using System;

public static class TestOptions
{
    public static void Configure(Action<int> configureOptions)
    {
    }
}

public class TestClass
{
    public void M()
    {
        TestOptions.Configure((int o) => Console.WriteLine(o));
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
