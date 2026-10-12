using Fdw.Collections;
using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// One connection type's entry point into the <see cref="DefaultValues"/> root collection.
/// </summary>
/// <remarks>
/// A root member is a dispatcher, not a policy — <see cref="ByName"/> resolves a policy name
/// against that connection type's own child collection (e.g. <c>MsSqlDefaults</c>). Resolve a
/// connection's configured policy as <c>DefaultValues.ByName(connectionType).ByName(policyName)</c>.
/// </remarks>
public interface IDefaultValueConnection : ITypeOption<int, DefaultValueConnectionBase>
{
    /// <summary>
    /// Resolves a policy name against this connection type's own policy collection.
    /// </summary>
    /// <param name="policyName">The policy name, e.g. <c>"Default"</c> or an organization's own.</param>
    /// <returns>The matching policy, or that collection's NotFound sentinel.</returns>
    IDefaultValuePolicy ByName(string policyName);

    /// <summary>
    /// Gets every policy registered for this connection type -- what a caller can name in a
    /// connection's <c>DefaultValuePolicy</c>. Empty when the connection type registers none.
    /// </summary>
    IReadOnlyList<IDefaultValuePolicy> Policies { get; }
}
