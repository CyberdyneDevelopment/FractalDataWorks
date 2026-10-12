using Fdw.Services.Calculations.Configuration;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Configuration for a Windowed calculation entity.
/// Specifies the target field, the window function to apply, and the output field name.
/// Partition-by and order-by field lists are stored in relational child tables
/// (<c>calc.WindowedCalculationPartitionField</c> / <c>calc.WindowedCalculationOrderField</c>).
/// Persisted in <c>calc.WindowedCalculationConfiguration</c> as a type-specific child of <c>calc.CalculationEntity</c>.
/// </summary>
/// <remarks>
/// Properties use <c>{ get; set; }</c> to satisfy IOptions binding requirements.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Calculation", ServiceType = "Windowed")]
public sealed partial class WindowedCalculationConfiguration : ICalculationTypedConfiguration, ICalculationEntityImplementationConfiguration
{

    /// <inheritdoc/>
    public string CalculationSource { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? OutputDataSetName { get; set; }

    /// <inheritdoc/>
    public string? ResultFieldName { get; set; }

    /// <inheritdoc/>
    public string ResultDataTypeName { get; set; } = "Decimal";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the inputs the calculation reads, held in <c>calc.WindowedCalculationInput</c>.</summary>
#pragma warning disable MA0016
    public List<WindowedCalculationInputConfiguration> Inputs { get; set; } = [];

    /// <summary>Gets or sets the steps the calculation executes, held in <c>calc.WindowedCalculationStep</c>.</summary>
    public List<WindowedCalculationStepConfiguration> Steps { get; set; } = [];
#pragma warning restore MA0016

    IReadOnlyList<CalculationInputConfigurationBase> ICalculationEntityImplementationConfiguration.Inputs => Inputs;

    IReadOnlyList<ICalculationStepConfiguration> ICalculationEntityImplementationConfiguration.Steps => Steps;

    /// <inheritdoc/>
    public void ReplaceInputs(IReadOnlyList<CalculationInput> inputs)
        => Inputs = [.. inputs.Select(CalculationInputConfigurationBase.From<WindowedCalculationInputConfiguration>)];

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <inheritdoc/>
    public Guid Id { get; set; }


    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the DataSet column that the window function operates on.
    /// </summary>
    public string TargetField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the window function to apply
    /// (e.g., <c>"Avg"</c>, <c>"Sum"</c>, <c>"RowNumber"</c>, <c>"Rank"</c>).
    /// </summary>
    public string WindowFunction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the field written to the output DataSet with the window result.
    /// </summary>
    public string OutputFieldName { get; set; } = string.Empty;
}
