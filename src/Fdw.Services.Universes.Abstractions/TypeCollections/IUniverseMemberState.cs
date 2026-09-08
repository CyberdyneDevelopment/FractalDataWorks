using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Where a membership stands: whether the person is actually here.</summary>
public interface IUniverseMemberState : ITypeOption<int, UniverseMemberStateBase>
{
    /// <summary>Gets whether a member in this state holds the access their role describes.</summary>
    bool GrantsMembership { get; }
}
