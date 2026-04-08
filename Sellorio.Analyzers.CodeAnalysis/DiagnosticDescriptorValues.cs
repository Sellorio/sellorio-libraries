using Microsoft.CodeAnalysis;

namespace Sellorio.Analyzers.CodeAnalysis
{
    public class DiagnosticDescriptorValues
    {
        public string Title { get; }
        public string MessageTemplate { get; }
        public string Category { get; }
        public DiagnosticSeverity Severity { get; }
        public bool IsEnabledByDefault { get; }

        public DiagnosticDescriptorValues(
            string title,
            string messageTemplate,
            string category = "Formatting",
            DiagnosticSeverity severity = DiagnosticSeverity.Warning,
            bool isEnabledByDefault = true)
        {
            Title = title;
            MessageTemplate = messageTemplate;
            Category = category;
            Severity = severity;
            IsEnabledByDefault = isEnabledByDefault;
        }
    }
}
