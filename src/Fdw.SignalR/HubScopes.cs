namespace Fdw.SignalR;

/// <summary>
/// The names of the group scopes every FDW real-time hub shares (FDW-2792).
/// </summary>
/// <remarks>
/// A publisher and the hub that joins a connection to a group must spell the group the same way, and the base class decides who may join a scope
/// by what its name says: a <c>user:</c> scope belongs to one person and an <c>org:</c> scope to one organization. Spelling them in one place keeps
/// the rule and the names from drifting apart.
/// </remarks>
public static class HubScopes
{
    /// <summary>The prefix of a scope that belongs to one person.</summary>
    public const string UserPrefix = "user:";

    /// <summary>The prefix of a scope that belongs to one organization.</summary>
    public const string OrgPrefix = "org:";

    /// <summary>Names the scope of one person.</summary>
    /// <param name="userId">The person's id.</param>
    /// <returns>The scope, <c>user:{userId}</c>.</returns>
    public static string User(string userId) => string.Concat(UserPrefix, userId);

    /// <summary>Names a scope of one organization.</summary>
    /// <param name="orgId">The organization's id.</param>
    /// <param name="kind">What the scope carries, for example <c>pipeline-updates</c>.</param>
    /// <returns>The scope, <c>org:{orgId}:{kind}</c>.</returns>
    public static string Org(string orgId, string kind) => string.Concat(OrgPrefix, orgId, ":", kind);
}
