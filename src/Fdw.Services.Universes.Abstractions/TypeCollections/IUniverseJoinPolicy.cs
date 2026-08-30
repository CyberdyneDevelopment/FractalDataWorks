using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>What happens when someone asks to join a universe.</summary>
public interface IUniverseJoinPolicy : ITypeOption<int, UniverseJoinPolicyBase>
{
}
