// Helper to create DiagnosticDescriptors with simplified parameters

using Microsoft.CodeAnalysis;

namespace Fdw.MessageLogging.Generators
{
    internal static class DiagnosticDescriptorHelper
    {
        /// <summary>
        /// Creates a DiagnosticDescriptor with simplified parameters.
        /// </summary>
        public static DiagnosticDescriptor Create(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            LocalizableString? description = null,
            string? helpLinkUri = null,
            params string[] customTags)
        {
            return new DiagnosticDescriptor(
                id,
                title,
                messageFormat,
                category,
                defaultSeverity,
                isEnabledByDefault,
                description,
                helpLinkUri,
                customTags);
        }
    }
}
