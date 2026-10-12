using Fdw.Collections;
using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for a connection type's root entry ("MsSql", "PostgreSql", ...) in <see cref="DefaultValues"/>.
/// </summary>
public abstract class DefaultValueConnectionBase : TypeOptionBase<DefaultValueConnectionBase>, IDefaultValueConnection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultValueConnectionBase"/> class.
    /// </summary>
    /// <param name="name">The connection type name, e.g. <c>"MsSql"</c>.</param>
    /// <remarks>
    /// Also the ctor the generator invokes to build the collection's NotFound sentinel, with a
    /// synthesized placeholder name — it does not require a separate parameterless overload.
    /// </remarks>
    protected DefaultValueConnectionBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Default body: no child collection to delegate to, so it returns a shared inert policy (never
    /// omits, always writes through). Real connection-type options override this to delegate into
    /// their own child collection. The base body exists so the generator's synthesized NotFound
    /// sentinel — a concrete subclass with no overrides of its own — is well-behaved rather than
    /// requiring one.
    /// </remarks>
    public virtual IDefaultValuePolicy ByName(string policyName) => UnregisteredPolicy;

    /// <inheritdoc />
    /// <remarks>
    /// Default body: no child collection, so nothing is registered. A connection-type option with a
    /// policy collection overrides this to list it, as it overrides <see cref="ByName"/> to resolve it.
    /// </remarks>
    public virtual IReadOnlyList<IDefaultValuePolicy> Policies => [];

    private static readonly IDefaultValuePolicy UnregisteredPolicy = new UnregisteredDefaultValuePolicy();

    private sealed class UnregisteredDefaultValuePolicy : DefaultValuePolicyBase
    {
        public UnregisteredDefaultValuePolicy()
            : base("Unregistered")
        {
        }
    }
}
