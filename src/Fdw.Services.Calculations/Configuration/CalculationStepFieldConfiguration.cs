using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// Relational replacement for <c>CalculationStep.GroupByFields</c> / <c>OrderByFields</c>.
/// Each row references a <c>DataSetField</c> by RowId with a role discriminator so a single
/// table covers both GROUP BY and ORDER BY semantics per step.
/// </summary>
/// <remarks>
/// Why: Fields referenced by RowId (not name) so lineage, rename, and RBAC compose correctly.
/// Implements IGenericConfiguration so [GenerateMapper] emits a cascade child descriptor for the
/// parent <see cref="CalculationStepConfiguration.Fields"/> collection (keystone compose + cascade-save).
/// </remarks>
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "CalculationStepField")]
public partial class CalculationStepFieldConfiguration : IGenericConfiguration
{
    /// <summary>Gets the configuration section name (computed; not a persisted column).</summary>
    public string SectionName => "CalculationStepFields";

    /// <summary>Gets the service type domain.</summary>
    public string ServiceType => "Calculation";

    /// <summary>Gets the service option type discriminator (none for step fields).</summary>
    public string? ServiceOptionType => null;


    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field role/name. Mirrors <see cref="StepFieldRole"/> to satisfy IGenericConfiguration.</summary>
    // Why: IGenericConfiguration requires a settable Name; a step field has no independent name, so it
    // mirrors the role discriminator. Not a persisted column (column-intersection drops it on save).
    public string Name { get => StepFieldRole; set => StepFieldRole = value; }

    /// <summary>Gets or sets the parent CalculationStep's logical Id.</summary>
    public Guid CalculationStepId { get; set; }



    /// <summary>Gets or sets the role of this field within the step: 'GroupBy' or 'OrderBy'.</summary>
    public string StepFieldRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the ordinal position within the role.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the sort direction for OrderBy fields ('Asc' or 'Desc'). Null for GroupBy fields.</summary>
    public string? Direction { get; set; }
}
