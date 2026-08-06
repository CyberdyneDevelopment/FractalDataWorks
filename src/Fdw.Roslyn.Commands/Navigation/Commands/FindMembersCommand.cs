using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to list all members of a type.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindMembers")]
public sealed class FindMembersCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindMembersCommand"/> class.
    /// </summary>
    public FindMembersCommand()
        : base("FindMembers", RoslynCommandCategories.Navigation, "List members declared on the type at FilePath + Line + Column. IncludeInherited brings in members inherited from base types; MemberKinds (comma-separated: 'Method,Property,Field,Event') filters by kind. Use to enumerate a type's surface as a precursor to refactoring. Returns MemberInfo entries with signature, kind, accessibility, and modifiers.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target type.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the target type within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the target type within FilePath.")]
    public int Column { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to include inherited members.
    /// </summary>
    [System.ComponentModel.Description("When true, include members inherited from base types; false (default) returns only members declared directly on the type.")]
    public bool IncludeInherited { get; init; }

    /// <summary>
    /// Gets or sets the member kinds filter (comma-separated: Method,Property,Field,Event).
    /// </summary>
    [System.ComponentModel.Description("Optional comma-separated filter of member kinds to include (e.g. 'Method,Property'). Valid kinds: Method, Property, Field, Event. Null/empty returns all kinds.")]
    public string? MemberKinds { get; init; }
}
