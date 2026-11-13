using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>An intended source of a Graph data set (FDW-5294): a data store or another data set it says it will read, before details are known.</summary>
/// <remarks>
/// Maps to <c>data.GraphDataSetIntent</c>, owned by one <c>data.GraphDataSetConfiguration</c> row through
/// <c>GraphDataSetConfigurationRowId</c>, as <see cref="GraphDataSetFieldConfiguration"/> is. <see cref="IntentKind"/> is the name of a
/// <see cref="GraphIntentKinds"/> option, which says whether the intent names a data store or a data set. An intent states no container,
/// mapping or filter: it never makes the data set readable and never feeds a field out. Binding the data set replaces the intents; the save
/// that binds carries none.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetIntentConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the stable logical identifier for this intent record, minted by the app before INSERT.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the <see cref="GraphIntentKinds"/> option: what this intent names.</summary>
    public string IntentKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the data store this intent reads from; set for a kind that names a data store, null otherwise. The store can be external.</summary>
    public string? DataStoreName { get; set; }

    /// <summary>Gets or sets the logical id of the data set this intent reads from; set for a kind that names a data set, null otherwise.</summary>
    public Guid? SourceDataSetId { get; set; }

    /// <summary>Gets or sets the name of the data set this intent reads from, kept so the intent still reads when that data set is not visible to the reader; set for a kind that names a data set, null otherwise.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets a note on what the intent is for.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets the position of the intent among its data set's intents.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the user id of the person who stated the intention (FDW-5452); set by the server when it is made and carried across saves, null only for one stated before authors were recorded.</summary>
    public Guid? AuthorUserId { get; set; }

    /// <summary>Gets or sets when the intention was stated (FDW-5452); set with <see cref="AuthorUserId"/>.</summary>
    public DateTimeOffset? AuthoredAt { get; set; }

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
