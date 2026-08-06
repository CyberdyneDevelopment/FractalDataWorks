using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.CodeBuilder.Analysis.CSharp.Helpers;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Generators;

/// <summary>
/// Result of running a generator pipeline.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
public class GeneratorPipelineResult
{
    /// <summary>
    /// The input compilation before generators ran.
    /// </summary>
    public Compilation InputCompilation { get; set; } = null!;

    /// <summary>
    /// The output compilation after all generators ran.
    /// </summary>
    public Compilation OutputCompilation { get; set; } = null!;

    /// <summary>
    /// Detailed results from the generator run.
    /// </summary>
    public MultiGeneratorTestHelper.MultiGeneratorResult GeneratorResult { get; set; } = null!;

    /// <summary>
    /// Services that were registered for the test.
    /// </summary>
    public IDictionary<string, object> RegisteredServices { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Gets a registered service by type.
    /// </summary>
    public TService? GetService<TService>() where TService : class
    {
        return ServiceRegistrationTestHelper.Get<TService>(OutputCompilation);
    }

    /// <summary>
    /// Gets all syntax trees added by generators.
    /// </summary>
    public IEnumerable<SyntaxTree> GetAddedSyntaxTrees()
    {
        return GeneratorResult.GetAddedSyntaxTrees();
    }

    /// <summary>
    /// Gets a specific generated source by hint name.
    /// </summary>
    public string GetGeneratedSource(string hintName)
    {
        return GeneratorResult.GetGeneratedSource(hintName);
    }

    /// <summary>
    /// Checks if the output compilation has a specific type.
    /// </summary>
    public bool HasType(string fullyQualifiedName)
    {
        return OutputCompilation.GetTypeByMetadataName(fullyQualifiedName) != null;
    }

    /// <summary>
    /// Gets a type symbol from the output compilation.
    /// </summary>
    public INamedTypeSymbol GetType(string fullyQualifiedName)
    {
        var type = OutputCompilation.GetTypeByMetadataName(fullyQualifiedName);
        if (type == null)
        {
            throw new InvalidOperationException($"Type '{fullyQualifiedName}' not found in output compilation");
        }
        return type;
    }
}
