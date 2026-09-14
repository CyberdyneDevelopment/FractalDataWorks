using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Base for the cardinality of a declared relationship between two of a dataverse's data sets.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; the database stores the option's NAME, so the
/// name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class DataverseRelationshipCardinalityBase
    : TypeOptionBase<DataverseRelationshipCardinalityBase>, IDataverseRelationshipCardinality
{
    /// <summary>Initializes a new instance of the <see cref="DataverseRelationshipCardinalityBase"/> class.</summary>
    /// <param name="name">The option name, which is the value persisted.</param>
    protected DataverseRelationshipCardinalityBase(string name) : base(name)
    {
    }
}
