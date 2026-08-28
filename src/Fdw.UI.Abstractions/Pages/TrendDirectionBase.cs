using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for trend direction for metrics.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class TrendDirectionBase : TypeOptionBase<int, TrendDirectionBase>, ITrendDirection
{
    /// <summary>
    /// Initializes a new instance of <see cref="TrendDirectionBase"/>.
    /// </summary>
    protected TrendDirectionBase(int id, string name) : base(id, name) { }
}
