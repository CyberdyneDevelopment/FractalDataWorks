using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Lookup implementation of a Transformation: joins its input to its lookup input on the keys it declares; the lookup data is whatever the pipeline binds to the lookup port.
/// Ports: input, lookup -> output. Rows live in <c>transform.LookupTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class LookupTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>The implementation name a Lookup transformation's domain row carries.</summary>
    public const string ImplementationName = "Lookup";

    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row; the table has no Id of its own.</remarks>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsSystemProvided { get; set; }


    /// <summary>Gets or sets the joins, each one key pair and the value it brings across.</summary>
    /// <remarks>One row each in <c>transform.LookupTransformationConfigurationLookup</c>.</remarks>
    [NotMapped]
    public IList<LookupTransformationLookupConfiguration> Lookups { get; set; } = new List<LookupTransformationLookupConfiguration>();
}
