using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Structured query capability — reads data from a named container with optional
/// field selection, filter clauses, sort, and paging.
/// The pipeline builder renders the composite <c>QueryCommandBuilder</c> component
/// (declared via <see cref="ICommandCapabilityType.BuilderComponentType"/>) rather
/// than a flat field list, because the structured query shape requires container
/// and field pickers that depend on connection schema metadata.
/// </summary>
/// <remarks>
/// ConfigurationFields is empty by design — all rendering is handled by the
/// QueryCommandBuilder component registered on BuilderComponentType.
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
            id: 1,
            name: "Query",
            displayName: "Structured Query",
            configurationFields: [],
            // Why: BuilderComponentType is resolved at runtime by the UI layer.
            // We reference the type by name via a string convention rather than a direct
            // typeof() reference to avoid a hard dependency from Connections.Abstractions
            // (netstandard2.0) on a Blazor UI package (net10.0). The builder looks up the
            // component by scanning assemblies for a class named QueryCommandBuilder that
            // implements ICommandCapabilityBuilder.
            builderComponentType: null)
    {
    }
}
