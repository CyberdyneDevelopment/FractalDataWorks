using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Configuration for a Formula calculation entity.
/// Carries the formula language discriminator, the formula source body,
/// and an execution timeout.
/// Persisted in <c>calc.FormulaCalculation</c> as a type-specific child of <c>calc.CalculationEntity</c>.
/// </summary>
/// <remarks>
/// Properties use <c>{ get; set; }</c> to satisfy IOptions binding requirements.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Calculation", ServiceType = "Formula")]
public sealed partial class FormulaCalculationConfiguration : ICalculationTypedConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;


    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent CalculationEntity's logical Id (FK to calc.CalculationEntity.Id).</summary>
    public Guid CalculationEntityId { get; set; }


    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language of the formula expression.
    /// Supported values: <c>"CSharp"</c>, <c>"Sql"</c>.
    /// Defaults to <c>"CSharp"</c>.
    /// </summary>
    public string FormulaLanguage { get; set; } = "CSharp";

    /// <summary>
    /// Gets or sets the formula source body to evaluate.
    /// Must be a non-empty expression valid in the selected <see cref="FormulaLanguage"/>.
    /// </summary>
    public string FormulaBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum execution time in seconds before the formula is cancelled.
    /// Defaults to <c>30</c> seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
