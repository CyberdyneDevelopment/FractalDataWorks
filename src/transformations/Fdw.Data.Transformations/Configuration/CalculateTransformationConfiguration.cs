using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Calculate implementation of a Transformation: adds computed columns to the records of its one input.
/// Ports: input -> output. Rows live in <c>transform.CalculateTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class CalculateTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>The implementation name a Calculate transformation's domain row carries.</summary>
    public const string ImplementationName = "Calculate";

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


    /// <summary>Gets or sets the computed columns, in ExecutionOrder.</summary>
    /// <remarks>One row each in <c>transform.CalculateTransformationConfigurationCalculation</c>.</remarks>
    [NotMapped]
    public IList<CalculateTransformationCalculationConfiguration> Calculations { get; set; } = new List<CalculateTransformationCalculationConfiguration>();
}
