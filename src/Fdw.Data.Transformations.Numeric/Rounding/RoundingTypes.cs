using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>The rounding modes a Round transform can be configured with.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RoundingTypeBase), typeof(IRoundingType), typeof(RoundingTypes))]
public sealed partial class RoundingTypes : TypeCollectionBase<RoundingTypeBase, IRoundingType>
{
}
