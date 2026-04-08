using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Design.NonPrivateFieldsAreNotAllowedAnalyzer,
    Sellorio.Analyzers.CodeFixes.Design.NonPrivateFieldsAreNotAllowedCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Design;

public class NonPrivateFieldsAreNotAllowedAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_PrivateField()
    {
        var source = @"
public class TestClass
{
    private int _field;
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_PublicField()
    {
        var source = @"
public class TestClass
{
    public int {|SE0004:_field|};
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_PublicStaticReadonlyField()
    {
        var source = @"
public class TestClass
{
    public static readonly string {|SE0005:_field|} = ""value"";
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_ConvertsFieldToPropertyAndRenamesReferences()
    {
        var source = @"
public class TestClass
{
    public int {|SE0004:_field|};

    public void SetValue(int value)
    {
        _field = value;
    }

    public int GetValue()
    {
        return _field;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public int Field { get; set; }

    public void SetValue(int value)
    {
        Field = value;
    }

    public int GetValue()
    {
        return Field;
    }
}";
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ConvertsStaticReadonlyFieldToGetterOnlyProperty()
    {
        var source = @"
public class TestClass
{
    public static readonly string {|SE0005:_value|} = ""value"";

    public static string GetValue()
    {
        return _value;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public static string Value { get; } = ""value"";

    public static string GetValue()
    {
        return Value;
    }
}";
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
