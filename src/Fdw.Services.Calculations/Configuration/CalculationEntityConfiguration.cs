using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// Aggregate configuration for the <c>calc.CalculationEntity</c> table — the header plus its composed
/// child collections (Inputs, Steps→{Fields,Operands}) and polymorphic typed body (Formula/Windowed).
/// </summary>
/// <remarks>
/// Why: the keystone <c>ImplementationConfigurationProviderBase</c> composes the full aggregate on read
/// (ComposeChildren for the nav collections, ComposeTypedBody for <see cref="Configuration"/> dispatched
/// on <see cref="ServiceOptionType"/>) and cascade-saves it on write — there is no per-domain hand-assembly.
/// Named without the "Managed" suffix so the cascade FK derives correctly: Strip("Configuration") =>
/// "CalculationEntity" => child FK column "CalculationEntityId" (matches the DDL).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
// Why Temporal: a calculation definition is the archetypal valid-time record. When a settlement is
// restated, the run must use the definition that GOVERNED the period being recomputed, not the one
// that happened to be current when someone last edited it — and those differ the moment a correction
// is entered mid-period but backdated to its start. Transaction-time history (CreateDate/ModifyDate,
// which every config already keeps) answers "what did we believe then" and would hand back the
// pre-correction definition, silently reproducing the very figure the restatement exists to fix.
[ManagedConfiguration( ServiceCategory = "Calculation", ServiceType = "Entity", Temporal = true)]
public partial class CalculationEntityConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Calculations";

    /// <inheritdoc />
    // Why: Matches ServiceCategory from [ManagedConfiguration] attribute for IOptions binding path.
    public string ServiceType => "Calculation";

    /// <inheritdoc />
    // Why: the typed-body discriminator the keystone ComposeTypedBody dispatches on. It mirrors
    // CalculationEntityType ("Formula"/"Windowed") so the matching registered typed provider is selected.
    public string? ServiceOptionType => CalculationEntityType;


    /// <summary>Gets or sets the unique identifier for this calculation entity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the calculation entity name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the calculation entity type name (e.g. "Formula", "Windowed").</summary>
    public string CalculationEntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the <c>CalculationSourceTypes</c> option that wrote this row
    /// (provenance — stamped by the writer, e.g. "Configuration" for the built-in write path).
    /// </summary>
    public string CalculationSource { get; set; } = string.Empty;

    /// <summary>Gets or sets the target DataSet name for output.</summary>
    public string? OutputDataSetName { get; set; }

    /// <summary>Gets or sets the result field name written to the output DataSet.</summary>
    public string? ResultFieldName { get; set; }

    /// <summary>Gets or sets the data type name for the result (e.g. "Decimal", "Int32").</summary>
    public string ResultDataTypeName { get; set; } = "Decimal";

    /// <summary>Gets or sets whether this calculation entity is active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the polymorphic typed body (FormulaCalculationConfiguration / WindowedCalculationConfiguration).
    /// Composed on read by the base ComposeTypedBody (dispatch on <see cref="ServiceOptionType"/>); cascade-saved
    /// by base.Save on insert. Not a column on calc.CalculationEntity.
    /// </summary>
    [NotMapped]
    public ICalculationTypedConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the input declarations. Composed from calc.CalculationEntityInput on read; cascade-saved on insert.
    /// </summary>
#pragma warning disable MA0016
    public List<CalculationEntityInputRecord> Inputs { get; set; } = [];

    /// <summary>
    /// Gets or sets the calculation steps (each owning Fields + Operands). Composed recursively from
    /// calc.CalculationStep on read; cascade-saved on insert.
    /// </summary>
    public List<CalculationStepConfiguration> Steps { get; set; } = [];
#pragma warning restore MA0016
}
