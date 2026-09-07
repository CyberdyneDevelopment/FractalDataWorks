using System;
using System.Collections.Generic;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>A declared relationship between two data sets in a universe.</summary>
/// <remarks>Shaped to match <c>DataflowEdgeDto</c>, plus <see cref="IsDefined"/>.</remarks>
public class UniverseMapEdgeDto
{
    /// <summary>Gets or sets the relationship's stable identity.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set this relationship reads from.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set this relationship reads to.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets what the relationship means — its cardinality.</summary>
    public string RelationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the relationship's own name.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the join field identities, where they have been chosen.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>Gets or sets whether both sides of the join name a field.</summary>
    /// <remarks>
    /// A stored fact rather than a derivation: universe.UniverseRelationship.LeftFieldId and
    /// RightFieldId are both nullable, so a null on either side IS "the join key is not defined".
    /// This is what lets a client draw the difference between two sets that are related and two
    /// sets that are related by something nobody has chosen yet — which is usually the reason a
    /// pipeline over them cannot run.
    /// </remarks>
    public bool IsDefined { get; set; }
}
