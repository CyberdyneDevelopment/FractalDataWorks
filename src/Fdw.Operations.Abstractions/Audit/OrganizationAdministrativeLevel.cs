using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// Organization: a division inside a tenant.
/// </summary>
[TypeOption(typeof(AdministrativeLevels), "Organization", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrganizationAdministrativeLevel : AdministrativeLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationAdministrativeLevel"/> class.
    /// </summary>
    public OrganizationAdministrativeLevel()
        : base("Organization", 2)
    {
    }
}
