using Fdw.Collections;

namespace Fdw.Services.Abstractions;

/// <summary>Where a member a record cites stands in its own domain (FDW-6221): one option of <see cref="CitedStates"/>.</summary>
public interface ICitedState : ITypeOption<int, ICitedState>
{
}
