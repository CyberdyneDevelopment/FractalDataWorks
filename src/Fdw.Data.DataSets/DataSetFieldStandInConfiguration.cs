using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A DataSet field's stand-in recipe — how a sketched field answers a query without real data.
/// </summary>
/// <remarks>
/// Maps to <c>data.DataSetFieldStandIn</c>. Carries no strategy-type column: which one of the
/// strategy tables (FixedValue, Sequence, NumericRange, DateRange, RegexPattern, WeightedPick,
/// DrawFromDataSet) is populated for this row IS which strategy is active — see
/// <c>StandInStrategies</c> for the closed set those table names are validated against. The
/// generated values are never stored, only this recipe and the seed, so two people looking at the
/// same sketched field see the same values.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSet", ServiceType = "DataSetFieldStandIn")]
public sealed partial class DataSetFieldStandInConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field this stand-in answers for (FK to data.DataSetField.Id).</summary>
    public Guid DataSetFieldId { get; set; }

    /// <summary>Gets or sets the fraction of generated values that should be null, or null to never generate null.</summary>
    public decimal? NullPercent { get; set; }

    /// <summary>Gets or sets the field's own random seed, or null to inherit the dataverse's.</summary>
    public string? Seed { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
