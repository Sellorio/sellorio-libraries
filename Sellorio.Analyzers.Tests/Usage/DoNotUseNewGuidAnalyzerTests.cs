using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Usage.DoNotUseNewGuidAnalyzer,
    Sellorio.Analyzers.CodeFixes.Usage.DoNotUseNewGuidCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Usage;

public class DoNotUseNewGuidAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenUsingGuidNewGuid()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
        return Guid.NewGuid();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenUsingGuidConstructor()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
return {|SE0031:new Guid()|};
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenUsingImplicitObjectCreation()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
Guid value = {|SE0031:new()|};
        return value;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_UseDefaultLiteral_WhenTargetTypeIsExplicit()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
Guid value = {|SE0031:new Guid()|};
        return value;
    }
}";
        var fixedSource = @"
using System;

public class TestClass
{
    public Guid Create()
    {
        Guid value = default;
        return value;
    }
}";

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "UseDefault",
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_UseDefaultExpression_WhenTargetTypeIsNotExplicit()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
var value = {|SE0031:new Guid()|};
        return value;
    }
}";
        var fixedSource = @"
using System;

public class TestClass
{
    public Guid Create()
    {
        var value = default(Guid);
        return value;
    }
}";

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "UseDefault",
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_UseDefaultExpression_WhenDefaultLiteralIsNotSupported()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
Guid value = {|SE0031:new Guid()|};
        return value;
    }
}";
        var fixedSource = @"
using System;

public class TestClass
{
    public Guid Create()
    {
        Guid value = default(Guid);
        return value;
    }
}";

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "UseDefault",
        };
        test.SolutionTransforms.Add((solution, projectId) =>
            solution.WithProjectParseOptions(projectId, parseOptions));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_UseGuidNewGuid()
    {
        var source = @"
using System;

public class TestClass
{
    public Guid Create()
    {
return {|SE0031:new Guid()|};
    }
}";
        var fixedSource = @"
using System;

public class TestClass
{
    public Guid Create()
    {
        return Guid.NewGuid();
    }
}";

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "UseNewGuid",
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFix_UseQualifiedGuidNewGuid_WhenTypeIsQualified()
    {
        var source = @"
public class TestClass
{
    public System.Guid Create()
    {
System.Guid value = {|SE0031:new()|};
        return value;
    }
}";
        var fixedSource = @"
public class TestClass
{
    public System.Guid Create()
    {
        System.Guid value = System.Guid.NewGuid();
        return value;
    }
}";

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = "UseNewGuid",
        };

        await test.RunAsync();
    }
}
