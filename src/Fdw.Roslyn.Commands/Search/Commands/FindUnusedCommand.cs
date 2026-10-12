using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to find unused types and members.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindUnused")]
public sealed class FindUnusedCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindUnusedCommand"/> class.
    /// </summary>
    public FindUnusedCommand()
        : base("FindUnused", RoslynCommandCategories.Search, "Find types and members in the solution that have zero references. Use to triage dead-code candidates; tune IncludePrivate (default true) and IncludeInternal (default false) for accessibility scope. Constructors, property accessors, Main methods, and members marked [UsedImplicitly]/[PublicAPI] are excluded. Public members are never flagged (they may be referenced externally). Returns up to MaxResults UnusedMemberInfo entries.")
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include private members.
    /// </summary>
    [System.ComponentModel.Description("When true (default), include private members in the unused scan.")]
    public bool IncludePrivate { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include internal members.
    /// </summary>
    [System.ComponentModel.Description("When true, include internal and protected-internal members. Defaults to false because internals may be referenced from InternalsVisibleTo assemblies.")]
    public bool IncludeInternal { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    [System.ComponentModel.Description("Upper bound on the number of returned unused-member entries (default 100).")]
    public int MaxResults { get; init; } = 100;
}
