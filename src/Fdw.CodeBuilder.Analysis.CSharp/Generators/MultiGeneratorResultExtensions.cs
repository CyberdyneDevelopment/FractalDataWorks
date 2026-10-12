using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.CodeBuilder.Analysis.CSharp.Helpers;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Generators;

/// <summary>
/// Extension methods for MultiGeneratorResult.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class MultiGeneratorResultExtensions
{
    /// <summary>
    /// Asserts that no errors were produced during generation.
    /// </summary>
    public static MultiGeneratorTestHelper.MultiGeneratorResult AssertNoErrors(
        this MultiGeneratorTestHelper.MultiGeneratorResult result)
    {
        var errors = result.AllDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n", errors);
            throw new InvalidOperationException($"Generator produced errors:\n{errorMessages}");
        }

        return result;
    }

    /// <summary>
    /// Asserts that the specified number of sources were generated.
    /// </summary>
    public static MultiGeneratorTestHelper.MultiGeneratorResult AssertGeneratedSourceCount(
        this MultiGeneratorTestHelper.MultiGeneratorResult result,
        int expectedCount)
    {
        var actualCount = result.GetAllGeneratedSources().Count();
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} generated sources but found {actualCount}");
        }

        return result;
    }

    /// <summary>
    /// Gets a generated source by hint name.
    /// </summary>
    public static string GetGeneratedSource(
        this MultiGeneratorTestHelper.MultiGeneratorResult result,
        string hintName)
    {
        var source = result.GetAllGeneratedSources()
            .FirstOrDefault(s => string.Equals(s.HintName, hintName, StringComparison.Ordinal));

        if (source.HintName == null)
        {
            var availableHints = string.Join(", ",
                result.GetAllGeneratedSources().Select(s => s.HintName));
            throw new InvalidOperationException(
                $"No generated source found with hint name '{hintName}'. " +
                $"Available: {availableHints}");
        }

        return source.SourceText.ToString();
    }
}
