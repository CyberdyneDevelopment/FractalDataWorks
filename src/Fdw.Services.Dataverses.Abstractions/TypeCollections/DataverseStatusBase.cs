using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Base for Where a dataverse is in its lifecycle.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; the database stores the option's NAME, so the
/// name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class DataverseStatusBase : TypeOptionBase<DataverseStatusBase>, IDataverseStatus
{
    /// <summary>Initializes a new instance of the <see cref="DataverseStatusBase"/> class.</summary>
    /// <param name="name">The option name, which is the value persisted.</param>
    protected DataverseStatusBase(string name) : base(name)
    {
    }
}
