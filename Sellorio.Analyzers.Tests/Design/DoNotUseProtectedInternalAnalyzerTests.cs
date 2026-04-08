using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Design.DoNotUseProtectedInternalAnalyzer,
    Sellorio.Analyzers.CodeFixes.Design.DoNotUseProtectedInternalCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Design;

public class DoNotUseProtectedInternalAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_ProtectedField()
    {
        var source = @"
public class TestClass
{
    protected int Field;
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_InternalField()
    {
        var source = @"
public class TestClass
{
    internal int Field;
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_PublicField()
    {
        var source = @"
public class TestClass
{
    public int Field;
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_PrivateField()
    {
        var source = @"
public class TestClass
{
    private int Field;
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ProtectedInternalField()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} int Field;
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ProtectedInternalMethod()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} void Method() { }
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_ProtectedInternalProperty()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} int Property { get; set; }
}";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_RemoveProtected_Field()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} int Field;
}";
        var fixedSource = @"
public class TestClass
{
    internal int Field;
}";
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "RemoveProtected",
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveInternal_Field()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} int Field;
}";
        var fixedSource = @"
public class TestClass
{
    protected int Field;
}";
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "RemoveInternal",
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveProtected_Method()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} void Method() { }
}";
        var fixedSource = @"
public class TestClass
{
    internal void Method() { }
}";
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "RemoveProtected",
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveInternal_Method()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} void Method() { }
}";
        var fixedSource = @"
public class TestClass
{
    protected void Method() { }
}";
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "RemoveInternal",
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveProtected_Property()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} int Property { get; set; }
}";
        var fixedSource = @"
public class TestClass
{
    internal int Property { get; set; }
}";
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "RemoveProtected",
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveInternal_Property()
    {
        var source = @"
public class TestClass
{
            {|SE0006:protected internal|} int Property { get; set; }
}";
        var fixedSource = @"
public class TestClass
{
    protected int Property { get; set; }
}";
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "RemoveInternal",
        };
        await test.RunAsync();
    }
}
