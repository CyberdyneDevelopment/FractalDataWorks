using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Concrete implementation of <see cref="ICalculationEntity"/> used for DataGateway-loaded entities.
/// </summary>
internal sealed class CalculationEntity : ICalculationEntity
{
    /// <inheritdoc/>
    public Guid Id { get; init; }

    /// <inheritdoc/>
    public string Name { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; init; }

    /// <inheritdoc/>
    public string CalculationEntityType { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string CalculationSource { get; init; } = string.Empty;

    /// <inheritdoc/>
    public IReadOnlyList<CalculationInput> Inputs { get; init; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<IGenericConfiguration> Steps { get; init; } = [];

    /// <inheritdoc/>
    public CalculationOutputSpec Output { get; init; } = new();

    /// <inheritdoc/>
    public bool IsEnabled { get; init; } = true;

    /// <inheritdoc/>
    public IGenericConfiguration? TypedConfiguration { get; init; }
}
