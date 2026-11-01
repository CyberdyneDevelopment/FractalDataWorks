using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One join of a Lookup transformation: the input's <see cref="SourceKeyField"/> matched to the lookup input's <see cref="LookupKeyField"/>.
/// Rows live in <c>transform.LookupTransformationConfigurationLookup</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class LookupTransformationLookupConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and position.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the key field on the input.</summary>
    public string SourceKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the key field on the lookup input.</summary>
    public string LookupKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the lookup input field brought across.</summary>
    public string LookupValueField { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional prefix for the fields brought across.</summary>
    public string? OutputFieldPrefix { get; set; }

    /// <summary>Gets or sets the join: an inner or left join, by name.</summary>
    public string JoinType { get; set; } = string.Empty;
}
