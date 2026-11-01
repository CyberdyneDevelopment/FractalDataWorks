using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Field implementation of a Transformation: one per-field operation, a
/// <see cref="Fdw.Data.Abstractions.TransformationTypes"/> option, with the values of the parameters it declares.
/// Rows live in <c>transform.FieldTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class FieldTransformationConfiguration : IFieldTransformationConfiguration
{
    /// <summary>The implementation name a Field transformation's domain row carries.</summary>
    public const string ImplementationName = "Field";

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
    public string TransformationType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool IsSystemProvided { get; set; }

    /// <inheritdoc/>
    /// <remarks>One row each in <c>transform.FieldTransformationConfigurationParameter</c>.</remarks>
    [NotMapped]
    public IList<FieldTransformationParameterConfiguration> Parameters { get; set; } = new List<FieldTransformationParameterConfiguration>();
}
