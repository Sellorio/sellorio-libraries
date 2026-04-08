using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Naming.LinqLambdaParameterNameAnalyzer,
    Sellorio.Analyzers.CodeFixes.Naming.LinqLambdaParameterNameCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Naming;

public class LinqLambdaParameterNameAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_FirstLevelLambdaNamedX()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => x + 1);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_NestedLambdasUsePreferredNames()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => values.Select(y => values.Select(z => values.Select(a => a + z + y + x))));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_FirstLevelLambdaUsesWrongName()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select({|SE0029:value|} => value + 1);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_SecondLevelLambdaUsesWrongName()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select(x => values.Select({|SE0029:item|} => item + x));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ThirdLevelLambdaUsesWrongName()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select(x => values.Select(y => values.Select({|SE0029:entry|} => entry + y + x)));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_FourthLevelLambdaUsesWrongName()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select(x => values.Select(y => values.Select(z => values.Select({|SE0029:node|} => node + z + y + x))));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_FifthLevelLambdaDoesNotTrigger()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => values.Select(y => values.Select(z => values.Select(a => values.Select(item => item + a + z + y + x)))));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_MultiParameterLinqLambda()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select((item, index) => item + index);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_CustomExtensionMethodNamedSelectOutsideSystemLinq()
    {
        var source = @"
using System;
using CustomLinq;

namespace CustomLinq
{
    public static class CustomEnumerableExtensions
    {
        public static TResult Select<TSource, TResult>(this TSource source, Func<TSource, TResult> selector)
        {
            return selector(source);
        }
    }
}

public class TestClass
{
    public void M(int value)
    {
        var result = value.Select(item => item + 1);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RenamesFirstLevelLambdaAndReferences()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select({|SE0029:value|} => value + value);
    }
}";
        var fixedSource = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => x + x);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesSecondLevelLambdaAndReferences()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select(x => values.Select({|SE0029:item|} => item + x + item));
    }
}";
        var fixedSource = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => values.Select(y => y + x + y));
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesThirdLevelLambdaAndReferences()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select(x => values.Select(y => values.Select({|SE0029:entry|} => entry + y + x + entry)));
    }
}";
        var fixedSource = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => values.Select(y => values.Select(z => z + y + x + z)));
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesFourthLevelLambdaAndReferences()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Select(x => values.Select(y => values.Select(z => values.Select({|SE0029:node|} => node + z + y + x + node))));
    }
}";
        var fixedSource = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Select(x => values.Select(y => values.Select(z => values.Select(a => a + z + y + x + a))));
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesParenthesizedLambdaParameter()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
var query = values.Where((int {|SE0029:item|}) => item > 0);
    }
}";
        var fixedSource = @"
using System.Linq;

public class TestClass
{
    public void M(int[] values)
    {
        var query = values.Where((int x) => x > 0);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
