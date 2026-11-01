namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The roles that may write and run scripts (FDW-1447).
/// </summary>
public static class ScriptRoles
{
    /// <summary>The role of a data engineer: the only role that may write or run a script.</summary>
    public const string DataEngineer = "DataEngineer";
}
