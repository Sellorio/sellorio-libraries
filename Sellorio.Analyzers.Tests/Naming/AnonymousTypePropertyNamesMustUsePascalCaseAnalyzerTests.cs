using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Naming.AnonymousTypePropertyNamesMustUsePascalCaseAnalyzer,
    Sellorio.Analyzers.CodeFixes.Naming.AnonymousTypePropertyNamesMustUsePascalCaseCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Naming;

public class AnonymousTypePropertyNamesMustUsePascalCaseAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_ExplicitPascalCaseProperty()
    {
        var source = @"
public class TestClass
{
    public object Create(int value)
    {
        return new { Value = value };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_ImplicitCamelCaseProperty()
    {
        var source = @"
public class TestClass
{
    public object Create(int value)
    {
        return new { value };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ExplicitCamelCaseProperty()
    {
        var source = @"
public class TestClass
{
    public object Create(int value)
    {
return new { {|SE0020:value|} = value };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_ImplicitUnderscoreProperty()
    {
        var source = @"
public class TestClass
{
    public object Create(int _value)
    {
        return new { _value };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RenamesExplicitProperty_ToPascalCase()
    {
        var source = @"
public class TestClass
{
    public object Create(int value)
    {
return new { {|SE0020:my_value|} = value };
    }
}";
        var fixedSource = @"
public class TestClass
{
    public object Create(int value)
    {
        return new { MyValue = value };
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
