using System.Threading.Tasks;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Performance.UseParseInsteadOfConvertAnalyzer,
    Sellorio.Analyzers.CodeFixes.Performance.UseParseInsteadOfConvertCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Performance;

public class UseParseInsteadOfConvertAnalyzerTests
{
    [Theory]
    [InlineData("ToBoolean", "bool")]
    [InlineData("ToByte", "byte")]
    [InlineData("ToSByte", "sbyte")]
    [InlineData("ToChar", "char")]
    [InlineData("ToInt16", "short")]
    [InlineData("ToUInt16", "ushort")]
    [InlineData("ToInt32", "int")]
    [InlineData("ToUInt32", "uint")]
    [InlineData("ToInt64", "long")]
    [InlineData("ToUInt64", "ulong")]
    [InlineData("ToSingle", "float")]
    [InlineData("ToDouble", "double")]
    [InlineData("ToDecimal", "decimal")]
    [InlineData("ToDateTime", "DateTime")]
    public async Task Diagnostic_SupportedConvertMethodWithStringParameter(string methodName, string typeName)
    {
        var source = $@"
using System;

public class TestClass
{{
    public {typeName} ParseValue(string value)
    {{
        return {{|SE0027:Convert.{methodName}(value)|}};
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("ToBoolean", "bool")]
    [InlineData("ToByte", "byte")]
    [InlineData("ToSByte", "sbyte")]
    [InlineData("ToChar", "char")]
    [InlineData("ToInt16", "short")]
    [InlineData("ToUInt16", "ushort")]
    [InlineData("ToInt32", "int")]
    [InlineData("ToUInt32", "uint")]
    [InlineData("ToInt64", "long")]
    [InlineData("ToUInt64", "ulong")]
    [InlineData("ToSingle", "float")]
    [InlineData("ToDouble", "double")]
    [InlineData("ToDecimal", "decimal")]
    [InlineData("ToDateTime", "DateTime")]
    public async Task CodeFix_ReplacesSupportedConvertMethodWithParse(string methodName, string typeName)
    {
        var source = $@"
using System;

public class TestClass
{{
    public {typeName} ParseValue(string value)
    {{
        return {{|SE0027:Convert.{methodName}(value)|}};
    }}
}}";
        var fixedSource = $@"
using System;

public class TestClass
{{
    public {typeName} ParseValue(string value)
    {{
        return {typeName}.Parse(value);
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesFullyQualifiedToDateTimeAndAddsUsingSystem()
    {
        var source = @"
public class TestClass
{
    public System.DateTime ParseValue(string value)
    {
        return {|SE0027:System.Convert.ToDateTime(value)|};
    }
}";
        var fixedSource = @"using System;

public class TestClass
{
    public System.DateTime ParseValue(string value)
    {
        return DateTime.Parse(value);
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoDiagnostic_ConvertMethodWithNonStringParameter()
    {
        var source = @"
using System;

public class TestClass
{
    public int ParseValue(double value)
    {
        return Convert.ToInt32(value);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_ConvertMethodWithMultipleParameters()
    {
        var source = @"
using System;

public class TestClass
{
    public int ParseValue(string value)
    {
        return Convert.ToInt32(value, 16);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
