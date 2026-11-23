using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A data set's own value of one federation strategy option (FDW-6901): ReadsInParallel, MaxParallelReads or OrdersSources. It wins over the
/// FDW-level setting and the value the strategy declares. Saved to <c>data.DataSetFederationOption</c> as a child of the Federated or Graph
/// data set's own row (one owner column per kind, as data.DataSetJoin is), so it is re-versioned and retired with its data set.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSetFederationOption")]
public sealed partial class DataSetFederationOptionConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name, the option's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the option's name: ReadsInParallel, MaxParallelReads or OrdersSources.</summary>
    public string OptionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text, parsed against the option's own type when the data set is saved and read.</summary>
    public string OptionValue { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
