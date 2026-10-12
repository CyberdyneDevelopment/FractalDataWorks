using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to find every type whose namespace disagrees with its file path and/or owning project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindNamespaceMismatches")]
public sealed class FindNamespaceMismatchesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindNamespaceMismatchesCommand"/> class.
    /// </summary>
    public FindNamespaceMismatchesCommand()
        : base("FindNamespaceMismatches", RoslynCommandCategories.Analysis, "Find every type whose namespace disagrees with its file path and/or its owning project, grouped by cause. Use as the read-only first step of a package split: the namespace already declares where a type belongs, so the mismatches ARE the move list — you never decide type by type. Scope narrows to a project or path; IncludeKinds filters to Path/Project/Both; IncludeTests defaults true. Groups come back WITHOUT their individual types by default because a solution-wide scan carrying every type is megabytes and gets truncated before it is read — set IncludeTypes (with Scope narrowed) to see individual files, and MaxTypesPerGroup to cap them. Returns groups (namespace, current project, expected project, nearest ancestor project, kind, counts) and flags each [TypeOption] because moving one between assemblies can empty a TypeCollection at runtime off a clean build.")
    {
    }

    /// <summary>
    /// Gets or sets the project name or path fragment to scan. Omit to scan the whole solution.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the mismatch kinds to include ("Path", "Project", "Both").
    /// </summary>
    /// <remarks>
    /// Empty includes every kind. A filter here narrows what is REPORTED, never what is scanned, so a
    /// narrowed run can still be trusted for the kinds it does report.
    /// </remarks>
    public string[] IncludeKinds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether test projects are scanned. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IncludeTests { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether each group carries its individual types. Defaults to
    /// <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// A solution-wide scan with every type attached is megabytes of JSON and is truncated before anyone
    /// reads it. Triage on the group counts first, then re-run with Scope narrowed and this set to see
    /// the individual files.
    /// </remarks>
    public bool IncludeTypes { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of types returned per group when <see cref="IncludeTypes"/> is set.
    /// Defaults to 200; anything dropped is counted in TypesOmitted rather than silently cut.
    /// </summary>
    public int MaxTypesPerGroup { get; set; } = 200;
}
