using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>The stored form of a version of a calculation held at an organization or a workspace (FDW-2560).</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Calculation", ServiceType = "CalculationVersion")]
public sealed partial class CalculationVersionImplementationConfiguration : ICalculationVersionImplementationConfiguration
{
    /// <inheritdoc />
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid BaseCalculationId { get; set; }

    /// <inheritdoc />
    public string CalculationName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ScopeKind { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid ScopeId { get; set; }

    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
}
