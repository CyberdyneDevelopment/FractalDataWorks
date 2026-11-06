using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// TypeCollection of the ways a Round node settles a midpoint. A developer adds a mode by adding an option that states the
/// <see cref="System.MidpointRounding"/> value it stands for.
/// </summary>
[TypeCollection(typeof(RoundingModeBase), typeof(IRoundingMode), typeof(RoundingModes))]
[ExcludeFromCodeCoverage]
public abstract partial class RoundingModes : TypeCollectionBase<RoundingModeBase, IRoundingMode>
{
}
