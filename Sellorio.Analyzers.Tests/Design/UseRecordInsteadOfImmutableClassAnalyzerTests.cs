using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Design.UseRecordInsteadOfImmutableClassAnalyzer,
    Sellorio.Analyzers.CodeFixes.Design.UseRecordInsteadOfImmutableClassCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Design;

public class UseRecordInsteadOfImmutableClassAnalyzerTests
{
    private const string IsExternalInitSource = @"
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}";

    [Fact]
    public async Task Diagnostic_ImmutableClassWithConstructorAssignments()
    {
        var source = @"
public class {|SE0011:Person|}
{
    public string Name { get; }
    public int Age { get; }

    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
}";

        await VerifyAnalyzerWithLanguageVersionAsync(source, LanguageVersion.Preview);
    }

    [Fact]
    public async Task CodeFix_ConvertsConstructorAssignedPropertiesToPositionalRecord()
    {
        var source = @"
public class {|SE0011:Person|}
{
    public string Name { get; }
    public int Age { get; }

    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
}";
        var fixedSource = @"
public record Person(string Name, int Age);";

        await VerifyCodeFixWithLanguageVersionAsync(source, fixedSource, LanguageVersion.Preview);
    }

    [Fact]
    public async Task CodeFix_ConvertsExpressionBodiedConstructorToPositionalRecord()
    {
        var source = @"
public class {|SE0011:NameHolder|}
{
    public string Name { get; }

    public NameHolder(string name) => Name = name;
}";
        var fixedSource = @"
public record NameHolder(string Name);";

        await VerifyCodeFixWithLanguageVersionAsync(source, fixedSource, LanguageVersion.Preview);
    }

    [Fact]
    public async Task CodeFix_PreservesModifiersConstraintsInterfacesAttributesAndMembers()
    {
        var source = @"
[System.Serializable]
public sealed partial class {|SE0011:Response|}<T> : System.IEquatable<Response<T>>
    where T : class
{
    [System.Obsolete]
    public T Value { get; }

    public Response(T value)
    {
        Value = value;
    }

    public bool Equals(Response<T> other) => true;
}";
        var fixedSource = @"
[System.Serializable]
public sealed partial record Response<T>([property: System.Obsolete] T Value) : System.IEquatable<Response<T>>
    where T : class
{
    public bool Equals(Response<T> other) => true;
}";

        await VerifyCodeFixWithLanguageVersionAsync(source, fixedSource, LanguageVersion.Preview);
    }

    [Fact]
    public async Task CodeFix_PreservesDelegatingConstructors()
    {
        var source = @"
public class {|SE0011:Customer|}
{
    public string Name { get; }

    public Customer()
        : this(""unknown"")
    {
    }

    public Customer(string name)
    {
        Name = name;
    }
}";
        var fixedSource = @"
public record Customer(string Name)
{
    public Customer()
        : this(""unknown"")
    {
    }
}";

        await VerifyCodeFixWithLanguageVersionAsync(source, fixedSource, LanguageVersion.Preview);
    }

    [Fact]
    public async Task NoDiagnostic_WhenLanguageVersionDoesNotSupportRecords()
    {
        var source = @"
public class Person
{
    public string Name { get; }

    public Person(string name)
    {
        Name = name;
    }
}";

        await VerifyAnalyzerWithLanguageVersionAsync(source, LanguageVersion.CSharp7_3);
    }

    [Fact]
    public async Task NoDiagnostic_WhenClassDerivesFromNonRecordBaseType()
    {
        var source = @"
public abstract class PersonBase
{
    protected PersonBase(string id)
    {
        Id = id;
    }

    public string Id { get; private set; }
}

public class Customer : PersonBase
{
    public string Name { get; }

    public Customer(string id, string name)
        : base(id)
    {
        Name = name;
    }
}";

        await VerifyAnalyzerWithLanguageVersionAsync(source, LanguageVersion.Preview);
    }

    private static async Task VerifyAnalyzerWithLanguageVersionAsync(string source, LanguageVersion languageVersion)
    {
        var test = new VerifyCS.Test
        {
            TestCode = source,
        };

        test.SolutionTransforms.Add((solution, projectId) =>
            solution.WithProjectParseOptions(projectId, CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
        await test.RunAsync(CancellationToken.None);
    }

    private static async Task VerifyCodeFixWithLanguageVersionAsync(string source, string fixedSource, LanguageVersion languageVersion)
    {
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.SolutionTransforms.Add((solution, projectId) =>
            solution.WithProjectParseOptions(projectId, CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));

        if (languageVersion >= LanguageVersion.CSharp9)
        {
            test.TestState.Sources.Add(IsExternalInitSource);
            test.FixedState.Sources.Add(IsExternalInitSource);
        }

        await test.RunAsync(CancellationToken.None);
    }
}
