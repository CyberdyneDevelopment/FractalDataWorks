using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for named default-value policy TypeOptions, one collection per connection type.
/// </summary>
public abstract class DefaultValuePolicyBase : TypeOptionBase<DefaultValuePolicyBase>, IDefaultValuePolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultValuePolicyBase"/> class.
    /// </summary>
    /// <param name="name">The policy name, e.g. <c>"Default"</c>.</param>
    /// <remarks>
    /// Also the ctor the generator invokes to build the collection's NotFound sentinel, with a
    /// synthesized placeholder name — it does not require a separate parameterless overload.
    /// </remarks>
    protected DefaultValuePolicyBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Default body: never omit, always write through whatever the caller gave (or null, if it gave
    /// nothing). Real policies override this; the base body exists so the generator's synthesized
    /// NotFound sentinel — a concrete subclass with no overrides of its own — is well-behaved rather
    /// than requiring one.
    /// </remarks>
    public virtual DefaultValueDecision Value(IField field, bool hasExplicitValue, object? explicitValue) =>
        DefaultValueDecision.Include(hasExplicitValue ? explicitValue : null);
}
