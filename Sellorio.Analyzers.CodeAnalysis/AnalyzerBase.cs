using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sellorio.Analyzers.CodeAnalysis
{
    public abstract class AnalyzerBase<TAnalyzer> : DiagnosticAnalyzer
        where TAnalyzer : AnalyzerBase<TAnalyzer>
    {
        private static ImmutableArray<DiagnosticDescriptor>? _diagnosticDescriptorCache;

        internal abstract Expression<Func<DiagnosticDescriptorValues>> Descriptor { get; }
        internal virtual Expression<Func<DiagnosticDescriptorValues>>[] AdditionalDescriptors => Array.Empty<Expression<Func<DiagnosticDescriptorValues>>>();

        protected DiagnosticDescriptor DiagnosticDescriptor => SupportedDiagnostics[0];
        protected DiagnosticDescriptor[] AdditionalDiagnosticDescriptors => SupportedDiagnostics.Skip(1).ToArray();

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetDiagnosticDescriptors();

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            RegisterActions(context);
        }

        protected abstract void RegisterActions(AnalysisContext context);

        private ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors()
        {
            if (_diagnosticDescriptorCache != null)
            {
                return _diagnosticDescriptorCache.Value;
            }

            var descriptorSelectors = new[] { Descriptor }.Concat(AdditionalDescriptors).Select(CreateDiagnosticDescriptor);
            var result = ImmutableArray.CreateRange(descriptorSelectors);

            _diagnosticDescriptorCache = result;

            return result;
        }

        private static DiagnosticDescriptor CreateDiagnosticDescriptor(Expression<Func<DiagnosticDescriptorValues>> descriptor)
        {
            var memberExpression = (MemberExpression)descriptor.Body;
            var propertyInfo = (PropertyInfo)memberExpression.Member;

            var id = propertyInfo.Name;
            var propertyDescriptorValues = (DiagnosticDescriptorValues)propertyInfo.GetValue(null);

            var diagnosticDescriptor =
                new DiagnosticDescriptor(
                    id: id,
                    title: propertyDescriptorValues.Title,
                    messageFormat: propertyDescriptorValues.MessageTemplate,
                    category: propertyDescriptorValues.Category,
                    defaultSeverity: propertyDescriptorValues.Severity,
                    isEnabledByDefault: propertyDescriptorValues.IsEnabledByDefault);

            return diagnosticDescriptor;
        }
    }
}
