using Fdw.Conventions;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>Text a migration guide writes where a move recorded no value (FDW-3580); never a valid assembly name or position.</summary>
[PlaceholderText]
public static class MigrationGuidePlaceholders
{
    /// <summary>Written when a move recorded no assembly or relative position.</summary>
    public const string NotRecorded = "(not recorded)";
}
