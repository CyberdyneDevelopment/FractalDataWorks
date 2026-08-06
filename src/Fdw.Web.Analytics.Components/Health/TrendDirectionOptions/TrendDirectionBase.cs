using Fdw.Collections;

namespace Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;

/// <summary>
/// Base class for trend direction TypeOptions.
/// </summary>
public abstract class TrendDirectionBase : TypeOptionBase<int, TrendDirectionBase>, ITrendDirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrendDirectionBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The trend direction name.</param>
    protected TrendDirectionBase(int id, string name)
        : base(id, name, $"TrendDirections:{name}", name, $"{name} trend direction", "Health")
    {
    }
}
