using System;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>A declared relationship between two of a universe's data sets.</summary>
public class UniverseRelationshipDto
{
    /// <summary>Gets or sets the relationship's logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the data set on the left.</summary>
    public Guid LeftDataSetId { get; set; }

    /// <summary>Gets or sets the left field, when the join key has been named.</summary>
    public Guid? LeftFieldId { get; set; }

    /// <summary>Gets or sets the data set on the right.</summary>
    public Guid RightDataSetId { get; set; }

    /// <summary>Gets or sets the right field, when the join key has been named.</summary>
    public Guid? RightFieldId { get; set; }

    /// <summary>Gets or sets the cardinality.</summary>
    public string Cardinality { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether both sides name a field.</summary>
    /// <remarks>Derived, never stored — the map draws a relationship whose key is not yet named.</remarks>
    public bool IsDefined => LeftFieldId.HasValue && RightFieldId.HasValue;
}
