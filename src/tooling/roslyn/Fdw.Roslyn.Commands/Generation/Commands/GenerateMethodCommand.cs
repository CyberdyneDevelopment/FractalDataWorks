using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate a method signature.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateMethod")]
public sealed class GenerateMethodCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateMethodCommand"/> class.
    /// </summary>
    public GenerateMethodCommand()
        : base("GenerateMethod", RoslynCommandCategories.Generation, "Generate a method stub on a target type given a signature. Use to scaffold a method declaration with default body and XML doc placeholder, ready to fill in. Returns the modified file location.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path where the method should be added.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the line number where to insert the method (1-based).
    /// </summary>
    public int Line { get; set; }
    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the name of the method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the return type of the method.
    /// </summary>
    public string ReturnType { get; set; } = "void";
    /// <summary>
    /// Gets or sets method parameters (e.g., 'string name, int age').
    /// </summary>
    public string? Parameters { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the method is async.
    /// </summary>
    public bool IsAsync { get; set; }
    /// <summary>
    /// Gets or sets the accessibility (public, private, protected, internal).
    /// </summary>
    public string Accessibility { get; set; } = "public";
}
