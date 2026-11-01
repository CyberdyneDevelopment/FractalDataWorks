using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to write the session's recorded change ledger to a migration-guide markdown file.
/// </summary>
[TypeOption(typeof(RoslynCommands), "WriteMigrationGuide")]
public sealed class WriteMigrationGuideCommand : RoslynCommandBase, ILedgerAwareCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteMigrationGuideCommand"/> class.
    /// </summary>
    public WriteMigrationGuideCommand()
        : base("WriteMigrationGuide", RoslynCommandCategories.Workspace, "Write the session's change ledger to a markdown migration guide. OutputPath is required and may be RELATIVE — a relative path resolves against the SOLUTION directory, not the server's working directory, so a repo path like 'PACKAGE-MIGRATION.md' lands in the same committed place every time. If that file already exists it is APPENDED to (a titled, timestamped section from SectionTitle), so one guide lives in the repo and accumulates per commit and a diff shows exactly what a change moved; pass Overwrite to replace it instead, which discards every prior section. Renames are marked consumer-breaking and moves are not, and cross-assembly moves get a type-to-package table for resolving CS0246. Returns the path written and the entry count.")
    {
    }

    /// <summary>
    /// Gets or sets the absolute path for the migration-guide markdown file.
    /// </summary>
    [System.ComponentModel.Description("Absolute path for the migration-guide markdown file.")]
    public string OutputPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the change ledger. Set by the handler before translation; excluded from JSON
    /// because the concrete ledger implementation is not a serializable data value.
    /// </summary>
    /// <summary>
    /// Gets or sets a value indicating whether to REPLACE the guide instead of appending to it.
    /// </summary>
    /// <remarks>
    /// Defaults to false: when the guide already exists it is appended to, so one file lives in the repo
    /// and accumulates per commit. Replacing a committed ledger destroys every prior section, so it has
    /// to be asked for explicitly rather than happening by default.
    /// </remarks>
    public bool Overwrite { get; set; }

    /// <summary>
    /// Gets or sets the section label for the appended section, e.g. "slice-1-vocabulary".
    /// </summary>
    public string? SectionTitle { get; set; }

    /// <summary>
    /// Gets or sets the change ledger, injected by the command handler.
    /// </summary>
    [JsonIgnore]
    public IChangeLedger? Ledger { get; set; }
}
