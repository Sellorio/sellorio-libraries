using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Naming.TupleElementNamesMustUsePascalCaseAnalyzer,
    Sellorio.Analyzers.CodeFixes.Naming.TupleElementNamesMustUsePascalCaseCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Naming;

public class TupleElementNamesMustUsePascalCaseAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_ExplicitPascalCaseTupleExpressionElementName()
    {
        var source = @"
public class TestClass
{
    public (int Value, int Count) Create(int value, int count)
    {
        return (Value: value, Count: count);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_ExplicitPascalCaseTupleTypeElementName()
    {
        var source = @"
public class TestClass
{
    public (int Value, int Count) Create(int value, int count)
    {
        return (Value: value, Count: count);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_CamelCaseTupleExpressionElementName()
    {
        var source = @"
public class TestClass
{
    public (int, int) Create(int value, int count)
    {
return ({|SE0018:myValue|}: value, Count: count);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_UnderscoreTupleTypeElementName()
    {
        var source = @"
public class TestClass
{
public (int {|SE0018:my_value|}, int Count) Create(int value, int count)
    {
        return (MyValue: value, Count: count);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RenamesTupleExpressionElement_ToPascalCase()
    {
        var source = @"
public class TestClass
{
    public (int, int) Create(int value, int count)
    {
return ({|SE0018:my_value|}: value, Count: count);
    }
}";
        var fixedSource = @"
public class TestClass
{
    public (int, int) Create(int value, int count)
    {
        return (MyValue: value, Count: count);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_RenamesTupleTypeElement_ToPascalCase()
    {
        var source = @"
public class TestClass
{
public (int {|SE0018:my_value|}, int Count) Create(int value, int count)
    {
        return (MyValue: value, Count: count);
    }
}";
        var fixedSource = @"
public class TestClass
{
    public (int MyValue, int Count) Create(int value, int count)
    {
        return (MyValue: value, Count: count);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
