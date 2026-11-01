namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The kinds of place in the scope tree, as the place ancestry names them (FDW-2512).
/// </summary>
public static class PlaceKinds
{
    /// <summary>A tenant, the top of the tree.</summary>
    public const string Tenant = "Tenant";

    /// <summary>An organization of a tenant.</summary>
    public const string Organization = "Organization";

    /// <summary>A workspace, of an organization or directly of its tenant.</summary>
    public const string Workspace = "Workspace";
}
