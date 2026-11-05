using System;
using Microsoft.CodeAnalysis;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// One collection kind for the NotFound sentinel guard tests: its generator, and a source declaring a collection of
/// that kind whose option base constructor takes the given extra parameters.
/// </summary>
/// <param name="Name">The kind's name, as the theory shows it.</param>
/// <param name="CollectionTypeName">The full name of the collection the source declares.</param>
/// <param name="CreateGenerator">Creates the kind's real generator.</param>
/// <param name="BuildSource">Builds the source from the extra constructor parameters, e.g. "System.Func&lt;string&gt; factory".</param>
internal sealed record SentinelGuardCase(
    string Name,
    string CollectionTypeName,
    Func<IIncrementalGenerator> CreateGenerator,
    Func<string, string> BuildSource);
