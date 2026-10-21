using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// A level of administration: the platform, a tenant, or an organization of a tenant.
/// </summary>
public interface IAdministrativeLevel : ITypeOption<int, AdministrativeLevelBase>
{
    /// <summary>
    /// Gets how far down the tree the level sits: 0 for the platform, 1 for a tenant, 2 for an organization.
    /// </summary>
    /// <remarks>
    /// Why stated: an administrator can act at their own level or below, never above, so the levels need an order that a
    /// rule can compare without naming them.
    /// </remarks>
    int Depth { get; }
}
