using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>What happens when someone asks to join a dataverse.</summary>
public interface IDataverseJoinPolicy : ITypeOption<int, DataverseJoinPolicyBase>
{
}
