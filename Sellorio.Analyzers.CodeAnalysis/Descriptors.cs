using Microsoft.CodeAnalysis;

namespace Sellorio.Analyzers.CodeAnalysis
{
    public static class Descriptors
    {
        public static DiagnosticDescriptorValues SE0001 { get; } = Create("Do not use unsafe keyword", "Do not use the 'unsafe' keyword.", "Security");
        public static DiagnosticDescriptorValues SE0002 { get; } = Create("Ternary operator using incorrect line breaks", "The ternary operator has line breaks in the wrong places. Line breaking should be before '?' and ':' (with indentation).", "Style");
        public static DiagnosticDescriptorValues SE0003 { get; } = Create("Private properties are not allowed", "Private properties are not allowed. Properties should only be used as a level of abstraction for consumer of the class or inheriting types.", "Design");
        public static DiagnosticDescriptorValues SE0004 { get; } = Create("Non-private fields are not allowed", "Non-private fields are not allowed as they allow uncontrolled modification of data.", "Design");
        public static DiagnosticDescriptorValues SE0005 { get; } = Create("Non-private static readonly fields are not allowed", "Instead of public static readonly fields, use static getter-only properties.", "Design");
        public static DiagnosticDescriptorValues SE0006 { get; } = Create("Do not use protected internal", "Do not use protected internal. This causes the symbol to be public within the assembly AND accessible to other assemblies that inherit from the class.", "Design");
        public static DiagnosticDescriptorValues SE0007 { get; } = Create("Use Any instead of Count or Length", "Use Any from System.Linq instead of {0}.", "Performance");
        public static DiagnosticDescriptorValues SE0008 { get; } = Create("Do not use the 'as' operator", "Do not use the 'as' operator. Instead either use an explicit cast or the 'is' operator.", "Usage");
        public static DiagnosticDescriptorValues SE0009 { get; } = Create("Arithmetic operators should be trailing", "Arithmetic operators (plus/minus/times/divide) should not be at the begining of the next line.", "Style");
        public static DiagnosticDescriptorValues SE0010 { get; } = Create("Don't use Nullable HasValue", "Use '== null' or '!= null' instead of HasValue.", "Style");
        public static DiagnosticDescriptorValues SE0011 { get; } = Create("Use record for immutable class", "Use a record instead of manually implementing an immutable class.", "Design");
        public static DiagnosticDescriptorValues SE0012 { get; } = Create("Extra whitespace after open brace", "Remove unnecessary whitespace.", "Style");
        public static DiagnosticDescriptorValues SE0013 { get; } = Create("Extra whitespace before close brace", "Remove unnecessary whitespace.", "Style");
        public static DiagnosticDescriptorValues SE0014 { get; } = Create("Multiline if-else should start on a new line", "A multiline if-else condition expression should begin on the following line to preseve intentation consistency.", "Style");
        public static DiagnosticDescriptorValues SE0015 { get; } = Create("Missing blank line before block", "There should be a blank line before block statements (e.g. method bodies, if/else blocks, loops, etc.) to improve readability.", "Style");
        public static DiagnosticDescriptorValues SE0016 { get; } = Create("Tuple expressions must have explicit naming", "Tuple expressions must have explicit naming for all elements to conform with naming standards and for consistency for values that cannot be implicitly named.", "Naming");
        public static DiagnosticDescriptorValues SE0017 { get; } = Create("Tuple declarations must have explicit naming", "Tuple declarations must have explicit naming for all elements to conform with naming standards and avoid Item1/Item2 meaningless names.", "Naming");
        public static DiagnosticDescriptorValues SE0018 { get; } = Create("Tuple element names must use PascalCase", "Tuple element '{0}' must use PascalCase naming convention.", "Naming");
        public static DiagnosticDescriptorValues SE0019 { get; } = Create("Anonymous type properties must be explicitly named", "Anonymous type properties must have explicit names to improve code readability and consistency.", "Naming");
        public static DiagnosticDescriptorValues SE0020 { get; } = Create("Anonymous type property names must use PascalCase", "Anonymous type property '{0}' must use PascalCase naming convention.", "Naming");
        public static DiagnosticDescriptorValues SE0021 { get; } = Create("Multi-line assignment or return should start on new line", "Multi-line {0} statements should have the expression start on a new line for consistency and readability.", "Style");
        public static DiagnosticDescriptorValues SE0022 { get; } = Create("Do not use ThreadStaticAttribute", "Do not use ThreadStaticAttribute. Data flow between threads should be managed using DI scopes or similar mechanisms.", "Design");
        public static DiagnosticDescriptorValues SE0023 { get; } = Create("Extension methods should not indicate null acceptance", "Extension method '{0}' has a name that suggests it accepts null values. Extension methods should act like instance methods and reject null values.", "Naming");
        public static DiagnosticDescriptorValues SE0024 { get; } = Create("Do not call extension methods that indicate null acceptance", "Extension method '{0}' has a name that suggests it accepts null values. Extension methods should act like instance methods and reject null values.", "Usage");
        public static DiagnosticDescriptorValues SE0025 { get; } = Create("Use single-line comments instead of multiline comments", "Use multiple single-line comments (//) instead of multiline comments (/* */).", "Style");
        public static DiagnosticDescriptorValues SE0026 { get; } = Create("Line is too long", "Lines should not be longer than 160 characters.", "Maintainability");
        public static DiagnosticDescriptorValues SE0027 { get; } = Create("Use Parse or TryParse instead of Convert", "Use '{0}.Parse' or '{0}.TryParse' instead of 'Convert.{1}' for converting strings.", "Performance");
        public static DiagnosticDescriptorValues SE0028 { get; } = Create("Lambda parameter for configure/options should be named 'o' or 'options'", "Lambda parameter '{0}' should be named 'o' or 'options' when passed to a parameter containing 'configure' or 'options'.", "Naming");
        public static DiagnosticDescriptorValues SE0029 { get; } = Create("Lambda parameter for LINQ methods should use preferred naming", "Lambda parameter '{0}' should be named '{1}' for this nesting level in LINQ method calls.", "Naming");
        public static DiagnosticDescriptorValues SE0030 { get; } = Create("No linebreaks after await", "Do not have a linebreak after the 'await' keyword. Move the start of the awaited expression to the same line.", "Style");
        public static DiagnosticDescriptorValues SE0031 { get; } = Create("Do not use new Guid()", "Do not use 'new Guid()'. Use 'default(Guid)' if an empty Guid is intended, or 'Guid.NewGuid()' if a random Guid is intended.", "Usage");
        public static DiagnosticDescriptorValues SE0032 { get; } = Create("Unnecessary await", "Await can be simplified.", "Usage");
        public static DiagnosticDescriptorValues SE0033 { get; } = Create("Close parenthesis must not be first on line", "Close parenthesis ')' should not be at the start of a line. Move it to the end of the previous line.", "Style");

        private static DiagnosticDescriptorValues Create(
            string title,
            string messageTemplate,
            string category,
            DiagnosticSeverity severity = DiagnosticSeverity.Warning,
            bool isEnabledByDefault = true)
                => new DiagnosticDescriptorValues(title, messageTemplate, "Sellorio" + category, severity, isEnabledByDefault);
    }
}
