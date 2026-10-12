using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Filter implementation of a Transformation: keeps the records of its one input that satisfy <see cref="FilterExpression"/>.
/// Ports: input -> output. Rows live in <c>transform.FilterTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class FilterTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>The implementation name a Filter transformation's domain row carries.</summary>
    public const string ImplementationName = "Filter";

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


    /// <summary>Gets or sets the predicate a record must satisfy to pass.</summary>
    public string FilterExpression { get; set; } = string.Empty;
}
