using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Performance.UseAnyInsteadOfCountOrLengthAnalyzer,
    Sellorio.Analyzers.CodeFixes.Performance.UseAnyInsteadOfCountOrLengthCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Performance;

public class UseAnyInsteadOfCountOrLengthAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_StringLengthComparedToZero()
    {
        var source = @"
public class TestClass
{
    public bool HasText(string value)
    {
        return value.Length > 0;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_LinqCountInvocationComparedToZero()
    {
        var source = @"
using System.Linq;

public class TestClass
{
    public bool HasValues(int[] values)
    {
        return values.Count() > 0;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_CountPropertyComparedToZero()
    {
        var source = @"
using System.Collections.Generic;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
return {|SE0007:values.Count|} > 0;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_LengthPropertyComparedToZero()
    {
        var source = @"
public class TestClass
{
    public bool HasValues(int[] values)
    {
return {|SE0007:values.Length|} != 0;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_ReplacesCountGreaterThanZero_WithAny()
    {
        var source = @"
using System.Collections.Generic;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
return {|SE0007:values.Count|} > 0;
    }
}";
        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
        return values.Any();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesLengthNotEqualsZero_WithAny()
    {
        var source = @"
public class TestClass
{
    public bool HasValues(int[] values)
    {
return {|SE0007:values.Length|} != 0;
    }
}";
        var fixedSource = @"using System.Linq;

public class TestClass
{
    public bool HasValues(int[] values)
    {
        return values.Any();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesCountEqualsZero_WithNegatedAny()
    {
        var source = @"
using System.Collections.Generic;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
return {|SE0007:values.Count|} == 0;
    }
}";
        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
        return !values.Any();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesZeroEqualsCount_WithNegatedAny()
    {
        var source = @"
using System.Collections.Generic;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
return 0 == {|SE0007:values.Count|};
    }
}";
        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public bool HasValues(List<int> values)
    {
        return !values.Any();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
