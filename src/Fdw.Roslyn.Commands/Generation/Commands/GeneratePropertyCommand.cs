using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate a property.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateProperty")]
public sealed class GeneratePropertyCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratePropertyCommand"/> class.
    /// </summary>
    public GeneratePropertyCommand()
        : base("GenerateProperty", RoslynCommandCategories.Generation, "Generate a property (auto, init-only, or full) on a target type. Use to scaffold a property declaration with optional getter/setter accessibility and default value. Returns the modified file location.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path where the property should be added.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the line number where to insert the property (1-based).
    /// </summary>
    public int Line { get; set; }
    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type of the property.
    /// </summary>
    public string PropertyType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether to include a getter.
    /// </summary>
    public bool HasGetter { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether to include a setter.
    /// </summary>
    public bool HasSetter { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether to use auto-property syntax.
    /// </summary>
    public bool IsAutoProperty { get; set; } = true;
    /// <summary>
    /// Gets or sets the backing field name (if not auto-property).
    /// </summary>
    public string? BackingFieldName { get; set; }
}
