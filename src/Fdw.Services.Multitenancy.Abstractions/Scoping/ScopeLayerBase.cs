using Fdw.Collections;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Scoping;

/// <summary>
/// Base for the dimensions a row is scoped by.
/// </summary>
public abstract class ScopeLayerBase : TypeOptionBase<int, IScopeLayer>, IScopeLayer
{
    /// <summary>Initializes a new instance of the <see cref="ScopeLayerBase"/> class.</summary>
    /// <param name="id">The option id.</param>
    /// <param name="name">The layer name.</param>
    /// <param name="claim">The claim this layer's value travels in.</param>
    /// <param name="columnName">The column carrying it on a scoped table.</param>
    /// <param name="sessionContextKey">The key it is stamped under in session context.</param>
    protected ScopeLayerBase(
        int id, string name, IClaimDefinition claim, string columnName, string sessionContextKey)
        : base(id, name)
    {
        Claim = claim;
        ColumnName = columnName;
        SessionContextKey = sessionContextKey;
    }

    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ScopeLayerBase()
        : base(0, "NotFound")
    {
        Claim = null;
        ColumnName = string.Empty;
        SessionContextKey = string.Empty;
    }

    /// <inheritdoc />
    public IClaimDefinition? Claim { get; }

    /// <inheritdoc />
    public string ColumnName { get; }

    /// <inheritdoc />
    public string SessionContextKey { get; }
}
