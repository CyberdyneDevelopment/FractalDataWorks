using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to add braces to single-line statements.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AddBraces")]
public sealed class AddBracesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddBracesCommand"/> class.
    /// </summary>
    public AddBracesCommand()
        : base("AddBraces", RoslynCommandCategories.Formatting, "Add braces to single-line if/else/while/for statements throughout the loaded solution. Use as a mechanical cleanup to enforce brace-always-required style; safe to run repeatedly. Returns the count and locations of statements that received braces.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
