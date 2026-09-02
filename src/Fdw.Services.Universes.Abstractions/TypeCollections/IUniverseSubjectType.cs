using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>What kind of thing holds a membership — a person or a role.</summary>
public interface IUniverseSubjectType : ITypeOption<int, UniverseSubjectTypeBase>
{
}
