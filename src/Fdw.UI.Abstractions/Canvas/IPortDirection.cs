using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Interface for port direction types (In / Out).
/// </summary>
/// <remarks>
/// Implemented as a TypeCollection so downstream assemblies can extend with additional directions
/// (e.g. "Bidirectional") without changing this contract. Compare against
/// <see cref="PortDirections.NotFound"/> — never compare against null.
/// </remarks>
public interface IPortDirection : ITypeOption<int, PortDirectionBase>
{
}
