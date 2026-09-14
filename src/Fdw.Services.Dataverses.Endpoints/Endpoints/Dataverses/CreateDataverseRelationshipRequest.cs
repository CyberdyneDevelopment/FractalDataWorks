using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Declares a relationship between two of a dataverse's data sets.</summary>
public class CreateDataverseRelationshipRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set on the left.</summary>
    public Guid LeftDataSetId { get; set; }

    /// <summary>Gets or sets the left field, when the join key is already known.</summary>
    public Guid? LeftFieldId { get; set; }

    /// <summary>Gets or sets the data set on the right.</summary>
    public Guid RightDataSetId { get; set; }

    /// <summary>Gets or sets the right field, when the join key is already known.</summary>
    public Guid? RightFieldId { get; set; }

    /// <summary>Gets or sets the cardinality.</summary>
    public string Cardinality { get; set; } = string.Empty;

    /// <summary>Gets or sets the relationship's own display label.</summary>
    public string? Label { get; set; }
}
