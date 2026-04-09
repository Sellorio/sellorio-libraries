using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Sellorio.Analyzers.Tests.Verifiers;

public static partial class VisualBasicCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    internal class Test : VisualBasicCodeRefactoringTest<TCodeRefactoring, DefaultVerifier>
    {
    }
}
