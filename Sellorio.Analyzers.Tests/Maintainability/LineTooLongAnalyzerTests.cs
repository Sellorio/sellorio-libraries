using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using VerifyCS = Sellorio.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Sellorio.Analyzers.CodeAnalysis.Maintainability.LineTooLongAnalyzer,
    Sellorio.Analyzers.CodeFixes.Maintainability.LineTooLongCodeFixProvider>;

namespace Sellorio.Analyzers.Tests.Maintainability;

public class LineTooLongAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenMethodContainsLineLongerThanLimit()
    {
        var typeName = new string('A', 140);
        var longLine = $"        {{|SE0026:var value = new {typeName}();|}}";
        var source = $@"
class {typeName}
{{
}}
class C
{{
    void M()
    {{
{longLine}
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Diagnostic_WhenLocalFunctionContainsLineLongerThanLimit()
    {
        var condition = new string('B', 70);
        var source = $@"
class C
{{
    void M(bool {condition}One, bool {condition}Two, bool {condition}Three, bool {condition}Four)
    {{
        void Local()
        {{
{{|SE0026:if ({condition}One && {condition}Two && {condition}Three && {condition}Four)|}}
            {{
            }}
        }}
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CodeFix_SplitsAfterAssignmentWhenThatIsEnough()
    {
        var typeName = new string('A', 140);
        var source = $@"
class {typeName}
{{
}}
class C
{{
    void M()
    {{
        {{|SE0026:var value = new {typeName}();|}}
    }}
}}";
        var fixedSource = $@"
class {typeName}
{{
}}
class C
{{
    void M()
    {{
        var value =
            new {typeName}();
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsInvocationArgumentsAcrossLines()
    {
        var arg1 = new string('A', 24) + "One";
        var arg2 = new string('B', 24) + "Two";
        var arg3 = new string('C', 24) + "Three";
        var arg4 = new string('D', 24) + "Four";
        var arg5 = new string('E', 24) + "Five";
        var arg6 = new string('F', 24) + "Six";
        var source = $@"
class C
{{
    void M(string {arg1}, string {arg2}, string {arg3}, string {arg4}, string {arg5}, string {arg6})
    {{
        void DoSomething(string a, string b, string c, string d, string e, string f)
        {{
        }}

        {{|SE0026:DoSomething({arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6});|}}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M(string {arg1}, string {arg2}, string {arg3}, string {arg4}, string {arg5}, string {arg6})
    {{
        void DoSomething(string a, string b, string c, string d, string e, string f)
        {{
        }}

        DoSomething(
            {arg1},
            {arg2},
            {arg3},
            {arg4},
            {arg5},
            {arg6});
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsStringConcatenationAcrossLines()
    {
        var source = $@"
class C
{{
    void M()
    {{
        {{|SE0026:var text = ""this is a really long string with lots of words that needs to be split up in order to make code more readable otherwise the code will not be readable and we want our code to be readable!"";|}}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M()
    {{
        var text =
            ""this is a really long string with lots of words that needs to be split up in order to make code more readable otherwise the code will not be "" +
            ""readable and we want our code to be readable!"";
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsLogicalConditionsAcrossLines()
    {
        var condition1 = new string('A', 35) + "One";
        var condition2 = new string('B', 35) + "Two";
        var condition3 = new string('C', 35) + "Three";
        var condition4 = new string('D', 35) + "Four";
        var source = $@"
class C
{{
    void M(bool {condition1}, bool {condition2}, bool {condition3}, bool {condition4})
    {{
        {{|SE0026:if ({condition1} && {condition2} && {condition3} && {condition4})|}}
        {{
        }}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M(bool {condition1}, bool {condition2}, bool {condition3}, bool {condition4})
    {{
        if ({condition1} &&
            {condition2} &&
            {condition3} &&
            {condition4})
        {{
        }}
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsMethodChainsBeforeDots()
    {
        var source = @"
class C
{
    void M(string input)
    {
        {|SE0026:var text = input.Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize();|}
    }
}";
        var fixedSource = @"
class C
{
    void M(string input)
    {
        var text =
            input
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_PrefersAssignmentBeforeInvocationArguments()
    {
        var arg1 = new string('A', 24) + "One";
        var arg2 = new string('B', 24) + "Two";
        var arg3 = new string('C', 24) + "Three";
        var arg4 = new string('D', 24) + "Four";
        var arg5 = new string('E', 24) + "Five";
        var arg6 = new string('F', 24) + "Six";
        var source = $@"
class C
{{
    void M(string {arg1}, string {arg2}, string {arg3}, string {arg4}, string {arg5}, string {arg6})
    {{
        string Combine(string a, string b, string c, string d, string e, string f) => a + b + c + d + e + f;

        {{|SE0026:var value = Combine({arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6});|}}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M(string {arg1}, string {arg2}, string {arg3}, string {arg4}, string {arg5}, string {arg6})
    {{
        string Combine(string a, string b, string c, string d, string e, string f) => a + b + c + d + e + f;

        var value =
            Combine(
                {arg1},
                {arg2},
                {arg3},
                {arg4},
                {arg5},
                {arg6});
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsArithmeticAcrossLinesAfterAssignment()
    {
        var first = new string('A', 45) + "One";
        var second = new string('B', 45) + "Two";
        var third = new string('C', 45) + "Three";
        var source = $@"
class C
{{
    void M(int {first}, int {second}, int {third})
    {{
        {{|SE0026:var sum = {first} + {second} + {third};|}}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M(int {first}, int {second}, int {third})
    {{
        var sum =
            {first} +
            {second} +
            {third};
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsStringLiteralWithoutWordBoundaryWhenNeeded()
    {
        var literal = new string('x', 190);
        var source = $@"
class C
{{
    void M()
    {{
        {{|SE0026:var text = ""{literal}"";|}}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M()
    {{
        var text =
            ""{new string('x', 144)}"" +
            ""{new string('x', 46)}"";
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_SplitsReturnStatementUsingMethodChainPriority()
    {
        var source = @"
class C
{
    string M(string input)
    {
        {|SE0026:return input.Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize().Trim().ToUpperInvariant().Normalize();|}
    }
}";
        var fixedSource = @"
class C
{
    string M(string input)
    {
        return
            input
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize()
                .Trim()
                .ToUpperInvariant()
                .Normalize();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_FactorsIndentationIntoLineLengthForAdditionalSplits()
    {
        var condition = new string('Q', 30) + "Condition";
        var source = $@"
class C
{{
    void M(bool {condition}One, bool {condition}Two, bool {condition}Three, bool {condition}Four)
    {{
        if (true)
        {{
            {{|SE0026:if ({condition}One && {condition}Two && {condition}Three && {condition}Four)|}}
            {{
            }}
        }}
    }}
}}";
        var fixedSource = $@"
class C
{{
    void M(bool {condition}One, bool {condition}Two, bool {condition}Three, bool {condition}Four)
    {{
        if (true)
        {{
            if ({condition}One &&
                {condition}Two &&
                {condition}Three &&
                {condition}Four)
            {{
            }}
        }}
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoCodeFix_WhenLineCannotBeSplitBelowLimit()
    {
        var identifier = new string('x', 170);
        var source = $@"
class C
{{
    void M(string {identifier})
    {{
        var text = {identifier};
    }}
}}";

        await VerifyNoCodeFixAsync(source);
    }

    private static async Task VerifyNoCodeFixAsync(string source)
    {
        using (var workspace = new AdhocWorkspace())
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var solution = workspace.CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp7_3))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddDocument(documentId, "Test.cs", source);

            var project = solution.GetProject(projectId);
            var document = project.GetDocument(documentId);
            var compilation = await project.GetCompilationAsync();
            var analyzer = new Sellorio.Analyzers.CodeAnalysis.Maintainability.LineTooLongAnalyzer();
            var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)).GetAnalyzerDiagnosticsAsync();

            Assert.Single(diagnostics);

            var actions = new List<CodeAction>();
            var codeFixProvider = new Sellorio.Analyzers.CodeFixes.Maintainability.LineTooLongCodeFixProvider();
            var context = new CodeFixContext(
                document,
                diagnostics[0],
                (action, _) => actions.Add(action),
                CancellationToken.None);

            await codeFixProvider.RegisterCodeFixesAsync(context);

            Assert.Empty(actions);
        }
    }
}
