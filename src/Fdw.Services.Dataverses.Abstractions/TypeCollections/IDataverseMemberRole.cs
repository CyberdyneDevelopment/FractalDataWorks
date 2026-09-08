using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>A role a person can hold in a dataverse.</summary>
public interface IDataverseMemberRole : ITypeOption<int, DataverseMemberRoleBase>
{
    /// <summary>Gets whether this role may change the dataverse it is held in.</summary>
    bool MayWrite { get; }
}
