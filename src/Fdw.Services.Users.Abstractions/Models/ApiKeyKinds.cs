namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// The two kinds of API key a user may hold. One resource carries both; the kind decides which
/// vault stores the key and which principal shape it authenticates as.
/// </summary>
public static class ApiKeyKinds
{
    /// <summary>A personal access token: the user acting as themselves.</summary>
    public const string User = "user";

    /// <summary>An agent key: the user's agent, which also carries the agent role.</summary>
    public const string Agent = "agent";
}
