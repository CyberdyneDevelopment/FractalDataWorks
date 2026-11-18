using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>An organization owns the rule. (FDW-6454).</summary>
[TypeOption(typeof(DataOptionOwnerKinds), "Organization")]
public sealed class OrganizationOwnerKind : DataOptionOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationOwnerKind"/> class.</summary>
    public OrganizationOwnerKind()
        : base("Organization")
    {
    }
}
