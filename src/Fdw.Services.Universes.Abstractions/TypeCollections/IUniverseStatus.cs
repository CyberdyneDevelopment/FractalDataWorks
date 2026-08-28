using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Where a universe is in its lifecycle.</summary>
public interface IUniverseStatus : ITypeOption<int, UniverseStatusBase>
{
}
