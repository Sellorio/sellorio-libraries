using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Naming.TupleExpressionValuesMustBeExplicitlyNamedAnalyzer,
    Sellorio.Analyzers.CodeFixes.Naming.TupleExpressionValuesMustBeExplicitlyNamedCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Naming;

public class TupleExpressionValuesMustBeExplicitlyNamedAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_ExplicitlyNamedTupleElements()
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
    public async Task NoDiagnostic_TargetTypeProvidesExplicitNames()
    {
        var source = @"
public class TestClass
{
    public (int Value, int Count) Create(int value, int count)
    {
        (int Value, int Count) tuple = (value, count);
        return tuple;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ImplicitIdentifierTupleElement()
    {
        var source = @"
public class TestClass
{
    public (int, int) Create(int value, int count)
    {
return ({|SE0016:value|}, Count: count);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_AddsExplicitPascalCaseName_ForCamelCaseIdentifier()
    {
        var source = @"
public class TestClass
{
    public (int, int) Create(int camelCaseValue, int count)
    {
return ({|SE0016:camelCaseValue|}, Count: count);
    }
}";
        var fixedSource = @"
public class TestClass
{
    public (int, int) Create(int camelCaseValue, int count)
    {
        return (CamelCaseValue: camelCaseValue, Count: count);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_AddsExplicitPascalCaseName_ForUnderscoreIdentifier()
    {
        var source = @"
public class TestClass
{
    public (int, int) Create(int _value, int count)
    {
return ({|SE0016:_value|}, Count: count);
    }
}";
        var fixedSource = @"
public class TestClass
{
    public (int, int) Create(int _value, int count)
    {
        return (Value: _value, Count: count);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_AddsExplicitPascalCaseName_ForMemberAccess()
    {
        var source = @"
public class Person
{
    public string lastName { get; set; }
}

public class TestClass
{
    public (string, int) Create(Person person, int count)
    {
return ({|SE0016:person.lastName|}, Count: count);
    }
}";
        var fixedSource = @"
public class Person
{
    public string lastName { get; set; }
}

public class TestClass
{
    public (string, int) Create(Person person, int count)
    {
        return (LastName: person.lastName, Count: count);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
