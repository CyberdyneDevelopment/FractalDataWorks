using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// Tenant: one company.
/// </summary>
[TypeOption(typeof(AdministrativeLevels), "Tenant", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TenantAdministrativeLevel : AdministrativeLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAdministrativeLevel"/> class.
    /// </summary>
    public TenantAdministrativeLevel()
        : base("Tenant", 1)
    {
    }
}
