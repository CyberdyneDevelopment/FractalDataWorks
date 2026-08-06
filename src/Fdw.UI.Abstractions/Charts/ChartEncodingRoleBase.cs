using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Base class for chart encoding role type options using the CRTP pattern.
/// </summary>
/// <remarks>
/// Inherit from this class and apply <c>[TypeOption(typeof(ChartEncodingRoles), "YourName")]</c>
/// to register a new encoding role. Flags are set via constructor arguments — no property overrides.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class ChartEncodingRoleBase : TypeOptionBase<int, ChartEncodingRoleBase>, IChartEncodingRole
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartEncodingRoleBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this encoding role.</param>
    /// <param name="name">The registry name (used by <c>ChartEncodingRoles.ByName()</c>).</param>
    /// <param name="displayName">The human-readable name shown in the field-binding UI.</param>
    /// <param name="isSpatial">Whether this role is a spatial (axis) channel.</param>
    // Why: TypeOptionBase already exposes DisplayName + Category; pass them through its
    // 6-arg ctor (id, name, configurationKey, displayName, description, category) rather than
    // redeclaring. IsSpatial is net-new.
    protected ChartEncodingRoleBase(int id, string name, string displayName, bool isSpatial)
        : base(id, name, name, displayName, displayName, category: null)
    {
        IsSpatial = isSpatial;
    }

    /// <inheritdoc />
    public bool IsSpatial { get; }
}
