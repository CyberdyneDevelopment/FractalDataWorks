using System.Collections.Generic;
using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The contract every DataSet implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be
/// registered against this domain or handed back by a read of it.
/// <para>
/// This is what every kind of data set has. What only some kinds have is on the kind's own contract:
/// <see cref="IJoinedDataSetConfiguration"/> (Compound and Federated), <see cref="ISimpleDataSetConfiguration"/>
/// (aggregates) and <see cref="IFederatedDataSetConfiguration"/> (the federation strategy). Each kind is its own
/// implementation with its own table (FDW-1031).
/// </para>
/// </remarks>
public interface IDataSetImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the data set's Description.</summary>
    string Description { get; set; }

    /// <summary>
    /// Gets or sets how sensitive this data is: the <c>ClassificationId</c> of a <c>DataClassifications</c>
    /// level. <c>NotYetClassified</c> until someone picks one; never empty. Maps to <c>ClassificationId</c> (FDW-938).
    /// </summary>
    int ClassificationId { get; set; }

    /// <summary>Gets or sets the data set's Fields.</summary>
    IList<DataSetFieldConfiguration> Fields { get; set; }

    /// <summary>Gets or sets the data set's Sources.</summary>
    IList<DataSetSourceConfiguration> Sources { get; set; }

    /// <summary>Gets or sets the data set's key fields.</summary>
    IList<DataSetKeyFieldConfiguration> KeyFields { get; set; }


    /// <summary>Gets or sets the display name.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the abbreviation.</summary>
    string? Abbreviation { get; set; }

    /// <summary>Gets or sets the is sortable.</summary>
    bool IsSortable { get; set; }

    /// <summary>Gets or sets the is filterable.</summary>
    bool IsFilterable { get; set; }

    /// <summary>Gets or sets the transform expression.</summary>
    string? TransformExpression { get; set; }

    /// <summary>Gets or sets the source data set name.</summary>
    string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the version.</summary>
    string Version { get; set; }

    /// <summary>Gets or sets the category.</summary>
    string Category { get; set; }

    /// <summary>Gets or sets the category id.</summary>
    Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    string RecordTypeName { get; set; }

    /// <summary>Gets or sets the caching.</summary>
    CachingConfiguration? Caching { get; set; }

    /// <summary>Gets or sets the filters.</summary>
    IList<DataSetFilterConditionConfiguration> Filters { get; set; }

    /// <summary>Gets the durable ids of this dataset's sources.</summary>
    IReadOnlyList<Guid> SourceIds { get; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    string ModifyOnBehalfOf { get; set; }
}
