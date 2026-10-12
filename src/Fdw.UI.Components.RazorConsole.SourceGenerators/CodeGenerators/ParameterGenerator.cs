using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Fdw.UI.Components.RazorConsole.SourceGenerators.CodeGenerators;

/// <summary>
/// Generates parameter properties from @code blocks.
/// </summary>
internal sealed class ParameterGenerator
{
    /// <summary>
    /// Generates parameter properties from @code blocks.
    /// </summary>
    /// <param name="document">The Razor code document to process.</param>
    /// <returns>Generated parameter code, or empty string if none.</returns>
    public static string Generate(RazorCodeDocument document)
    {
        var builder = new StringBuilder();

        // Extract @code blocks and find [Parameter] properties
        // Generate corresponding property declarations

        return builder.ToString();
    }
}
