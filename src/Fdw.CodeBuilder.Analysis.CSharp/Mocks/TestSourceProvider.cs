using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// Helper class for creating test sources.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class TestSourceProvider
{
    /// <summary>
    /// Creates a simple class source for testing.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="attributes">The attributes to apply to the class.</param>
    /// <returns>The source code for the class.</returns>
    public static string CreateClassSource(string className, string @namespace, params string[] attributes)
    {
        var attributesSource = string.Join("\r\n", attributes.Select(a => $"[{a}]"));

        return $$"""

				 namespace {{@namespace}};

				 {{attributesSource}}
				 public class {{className}}
				 {
				 }
				 """;
    }

    /// <summary>
    /// Creates a simple enum source for testing.
    /// </summary>
    /// <param name="enumName">The enum name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="values">The enum values.</param>
    /// <returns>The source code for the enum.</returns>
    public static string CreateEnumSource(string enumName, string @namespace, params string[] values)
    {
        var valuesSource = string.Join(",\r\n    ", values);

        return $$"""

				 namespace {{@namespace}};

				 public enum {{enumName}}
				 {
				     {{valuesSource}}
				 }
				 """;
    }
}
