using System.Collections.Generic;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>How much of a dataverse is backed by real data.</summary>
/// <remarks>
/// Counts and blocking names, deliberately no percentage. A ratio invites reading 75% as
/// mostly-fine when the missing quarter is the thing that blocks everything; the counts and the
/// names of what is blocking say the same thing without inviting that reading.
///
/// A single object rather than a list envelope: this is one dataverse's readiness.
/// </remarks>
public class DataverseReadinessResponse
{
    /// <summary>Gets or sets how many data sets have every bindable field bound.</summary>
    public int Bound { get; set; }

    /// <summary>Gets or sets how many data sets have some bindable fields bound and some not.</summary>
    public int Partial { get; set; }

    /// <summary>Gets or sets how many data sets have fields with types but nothing bound.</summary>
    public int Sketched { get; set; }

    /// <summary>Gets or sets how many data sets are named and nothing more — no fields, or fields with no types.</summary>
    public int Proposed { get; set; }

    /// <summary>Gets or sets how many individual fields are bound to a container field.</summary>
    public int FieldsBound { get; set; }

    /// <summary>Gets or sets how many fields could be bound at all.</summary>
    /// <remarks>
    /// EXCLUDES calculated fields. A calculated field has no source by design — it is not missing a
    /// binding, it does not have one — so counting it against the total would leave every data set
    /// containing a calculation permanently short of complete, and the measure would stop meaning
    /// anything.
    /// </remarks>
    public int FieldsDefined { get; set; }

    /// <summary>Gets or sets whether the dataverse can answer a query at all, stand-ins included.</summary>
    public bool Queryable { get; set; }

    /// <summary>Gets or sets whether the dataverse can produce physical tables.</summary>
    /// <remarks>
    /// Separate from <see cref="Queryable"/> on purpose: a federated data set is queryable and never
    /// materializable, and that is a property of its kind rather than a gap anybody can close.
    /// </remarks>
    public bool Materializable { get; set; }

    /// <summary>Gets or sets the names of the data sets preventing materialization.</summary>
    public IList<string> BlockedBy { get; set; } = [];
}
