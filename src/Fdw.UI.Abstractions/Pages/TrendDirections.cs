using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for trend direction values.
/// </summary>
[TypeCollection(typeof(TrendDirectionBase), typeof(ITrendDirection), typeof(TrendDirections))]
[ExcludeFromCodeCoverage]
public abstract partial class TrendDirections : TypeCollectionBase<TrendDirectionBase, ITrendDirection> { }
