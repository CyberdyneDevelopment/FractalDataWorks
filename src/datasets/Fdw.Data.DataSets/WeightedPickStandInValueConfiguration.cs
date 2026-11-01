using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>One weighted option within a <see cref="WeightedPickStandInConfiguration"/>.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSet", ServiceType = "WeightedPickStandInValue")]
public sealed partial class WeightedPickStandInValueConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pick this option belongs to (FK to data.WeightedPickStandIn.Id).</summary>
    public Guid WeightedPickStandInId { get; set; }

    /// <summary>Gets or sets the value this option generates when picked.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets how likely this option is relative to the pick's other options.</summary>
    public int Weight { get; set; }

    /// <summary>Gets or sets this option's display order.</summary>
    public int Ordinal { get; set; }

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
