using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate a constructor for a class.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateConstructor")]
public sealed class GenerateConstructorCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateConstructorCommand"/> class.
    /// </summary>
    public GenerateConstructorCommand()
        : base("GenerateConstructor", RoslynCommandCategories.Generation, "Generate a constructor for an existing class, capturing readonly fields as parameters. Use to wire up dependency injection or value-object initialization without writing the ceremony by hand. Returns the modified file location.")
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
    /// Gets or sets a value indicating whether to include readonly fields as parameters.
    /// </summary>
    public bool IncludeReadonlyFields { get; set; } = true;
}
