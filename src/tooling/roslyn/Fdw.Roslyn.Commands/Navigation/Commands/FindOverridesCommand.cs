using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to find all overrides of a virtual or abstract method.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindOverrides")]
public sealed class FindOverridesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindOverridesCommand"/> class.
    /// </summary>
    public FindOverridesCommand()
        : base("FindOverrides", RoslynCommandCategories.Navigation, "Find every override of the virtual/abstract method at FilePath + Line + Column across the solution. Use to assess the impact of changing a virtual method's signature or contract. Returns OverrideInfo entries with file/line and the overriding type's name.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the virtual or abstract member.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the member within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the member within FilePath.")]
    public int Column { get; init; }
}
