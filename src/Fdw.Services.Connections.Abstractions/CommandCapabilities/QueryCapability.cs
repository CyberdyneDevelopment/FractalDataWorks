using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Structured query capability — reads data from a named container with optional
/// field selection, filter clauses, sort, and paging.
/// </summary>
/// <remarks>
/// ConfigurationFields is empty by design: the structured query shape needs container
/// and field pickers driven by connection schema metadata, so the client edits it with
/// its own editor rather than a flat field list.
/// At runtime the task Configuration dict carries:
/// Container, Fields (comma-separated or "*"), FilterJson, SortJson, Skip, Take.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Query", RestrictToCurrentCompilation = true)]
public sealed class QueryCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCapability"/> class.
    /// </summary>
    public QueryCapability()
        : base(
            name: "Query",
            displayName: "Structured Query",
            configurationFields: [])
    {
    }
}
