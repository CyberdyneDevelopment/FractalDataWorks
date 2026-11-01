using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A named policy for how one connection type decides, per field, whether to omit a column on
/// write or send an explicit value.
/// </summary>
/// <remarks>
/// A member of a connection type's own child collection (e.g. <c>MsSqlDefaults</c>), never of the
/// root <see cref="IDefaultValueConnection"/> collection directly. The framework ships one policy
/// per connection type named <c>"Default"</c>; an organization ships its own by adding a
/// <c>[TypeOption]</c> against that same connection type's collection from its own assembly — no
/// change to FDW is required.
/// </remarks>
public interface IDefaultValuePolicy : ITypeOption<int, DefaultValuePolicyBase>
{
    /// <summary>
    /// Decides what should happen for one field on one write.
    /// </summary>
    /// <param name="field">The field being written.</param>
    /// <param name="hasExplicitValue">
    /// Whether the caller explicitly supplied a value for this field on this write — never inferred
    /// from the value itself (a legitimate explicit zero/empty/false is indistinguishable from "not
    /// set" by value alone).
    /// </param>
    /// <param name="explicitValue">The caller's value, meaningful only when <paramref name="hasExplicitValue"/> is <see langword="true"/>.</param>
    DefaultValueDecision Value(IField field, bool hasExplicitValue, object? explicitValue);
}
