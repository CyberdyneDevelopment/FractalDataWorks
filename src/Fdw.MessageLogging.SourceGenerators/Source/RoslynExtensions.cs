// Helper extensions to replace Microsoft.CodeAnalysis.DotnetRuntime.Extensions

using Microsoft.CodeAnalysis;

namespace Fdw.MessageLogging.Generators
{
    internal static class RoslynExtensions
    {
        /// <summary>
        /// Gets the best matching type by metadata name, handling generic types correctly.
        /// </summary>
        public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
        {
            // Try the simple case first
            var type = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
            if (type != null)
            {
                return type;
            }

            // Handle cases where the type might be in multiple assemblies
            // by checking all referenced assemblies
            foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                type = assembly.GetTypeByMetadataName(fullyQualifiedMetadataName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
