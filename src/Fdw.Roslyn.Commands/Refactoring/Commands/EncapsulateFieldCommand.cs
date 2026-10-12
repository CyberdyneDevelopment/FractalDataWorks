using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to encapsulate a field as a property.
/// </summary>
[TypeOption(typeof(RoslynCommands), "EncapsulateField")]
public sealed class EncapsulateFieldCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncapsulateFieldCommand"/> class.
    /// </summary>
    public EncapsulateFieldCommand()
        : base("EncapsulateField", RoslynCommandCategories.Refactoring, "Promote a public field to a property with a backing field. Use to enforce encapsulation as part of an API cleanup; all reference sites are rewritten. Returns the modified-files count and the new property name.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the name for the property (optional).
    /// </summary>
    public string? PropertyName { get; set; }
}
