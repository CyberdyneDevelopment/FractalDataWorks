using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;

/// <summary>
/// TypeCollection for trend directions.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TrendDirectionBase), typeof(ITrendDirection), typeof(TrendDirections))]
public sealed partial class TrendDirections : TypeCollectionBase<TrendDirectionBase, ITrendDirection>
{
}
