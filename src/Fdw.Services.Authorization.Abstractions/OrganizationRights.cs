namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The rights an organization route is judged by at the organization's scope (FDW-2840): the ones the routes' policies already name.
/// </summary>
public static class OrganizationRights
{
    /// <summary>Reads an organization's members, roles, settings and connections.</summary>
    public const string Read = "organizations:read";

    /// <summary>Changes an organization's members, roles, settings and connections.</summary>
    public const string Write = "organizations:write";
}
