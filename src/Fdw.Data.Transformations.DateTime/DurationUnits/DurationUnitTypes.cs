using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// TypeCollection of duration unit options available to field transforms.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DurationUnitTypeBase), typeof(IDurationUnitType), typeof(DurationUnitTypes))]
public sealed partial class DurationUnitTypes : TypeCollectionBase<DurationUnitTypeBase, IDurationUnitType>
{
}
