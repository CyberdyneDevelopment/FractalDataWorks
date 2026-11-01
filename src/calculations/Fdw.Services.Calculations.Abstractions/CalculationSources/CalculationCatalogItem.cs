using System;
using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// A single entry in the unified calculation catalog, tagged with the <see cref="CalculationSource"/>
/// that produced it (provenance, not a pointer to codified behavior).
/// </summary>
/// <remarks>
/// Replaces <c>IConfiguredCalculationType</c>. Sealed data carrier — no polymorphism, no IsCodeDefined
/// flag; the writing source's <see cref="CalculationSource"/> name IS the provenance.
/// </remarks>
public sealed class CalculationCatalogItem
{
    /// <summary>Gets the name of this calculation (unique within its source, case-insensitive).</summary>
    public required string Name { get; init; }

    /// <summary>Gets a display-friendly name for UI presentation.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets a human-readable description of what this calculation does.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the name of the <see cref="CalculationSourceTypes"/> option that wrote/owns this entry
    /// (e.g. "Default", "Configuration").
    /// </summary>
    public required string CalculationSource { get; init; }

    /// <summary>
    /// Gets the <c>calc.CalculationEntity</c> identifier when this item is backed by a configured
    /// entity; <see langword="null"/> for codified entries.
    /// </summary>
    public Guid? CalculationEntityId { get; init; }

    /// <summary>
    /// Gets the codified <c>CalculationTypes</c> operator identifier when this item is backed by a
    /// code-defined scalar operator; <see langword="null"/> for configured entries.
    /// </summary>
    public int? OperatorId { get; init; }

    /// <summary>Gets the names of the input fields this calculation requires.</summary>
    public IReadOnlyList<string> RequiredInputFields { get; init; } = [];

    /// <summary>Gets the output field name produced by this calculation.</summary>
    public required string OutputField { get; init; }

    /// <summary>Gets whether this calculation is currently active.</summary>
    public required bool IsEnabled { get; init; }
}
