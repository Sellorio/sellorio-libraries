using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Naming.AnonymousTypePropertiesMustBeExplicitlyNamedAnalyzer,
    Sellorio.Analyzers.CodeFixes.Naming.AnonymousTypePropertiesMustBeExplicitlyNamedCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Naming;

public class AnonymousTypePropertiesMustBeExplicitlyNamedAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_ExplicitlyNamedProperty()
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
    public async Task Diagnostic_ImplicitIdentifierProperty()
    {
        var source = @"
public class TestClass
{
    public object Create(int value)
    {
return new { {|SE0019:value|} };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ImplicitCamelCaseProperty_UsesAG0019()
    {
        var source = @"
public class TestClass
{
    public object Create(int camelCaseValue)
    {
return new { {|SE0019:camelCaseValue|} };
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
    public object Create(int value)
    {
return new { {|SE0019:value|} };
    }
}";
        var fixedSource = @"
public class TestClass
{
    public object Create(int value)
    {
        return new { Value = value };
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
    public object Create(int _value)
    {
return new { {|SE0019:_value|} };
    }
}";
        var fixedSource = @"
public class TestClass
{
    public object Create(int _value)
    {
        return new { Value = _value };
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
    public object Create(Person person)
    {
return new { {|SE0019:person.lastName|} };
    }
}";
        var fixedSource = @"
public class Person
{
    public string lastName { get; set; }
}

public class TestClass
{
    public object Create(Person person)
    {
        return new { LastName = person.lastName };
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
