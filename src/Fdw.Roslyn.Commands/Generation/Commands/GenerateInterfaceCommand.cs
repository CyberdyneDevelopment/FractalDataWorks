using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate an interface.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateInterface")]
public sealed class GenerateInterfaceCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateInterfaceCommand"/> class.
    /// </summary>
    public GenerateInterfaceCommand()
        : base("GenerateInterface", RoslynCommandCategories.Generation, "Generate a new interface, either standalone or extracted from a class's public surface. Use to expose a contract without manually transcribing the signatures. Returns the new interface's file path.")
    {
    }
    /// <summary>
    /// Gets or sets the name of the interface.
    /// </summary>
    public string InterfaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the namespace for the interface.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets method signatures (semicolon-separated).
    /// </summary>
    public string? Methods { get; set; }
    /// <summary>
    /// Gets or sets property definitions (semicolon-separated).
    /// </summary>
    public string? Properties { get; set; }
    /// <summary>
    /// Gets or sets the project name where the interface should be added.
    /// </summary>
    public string? ProjectName { get; set; }
    /// <summary>
    /// Gets or sets the file path where the interface should be created.
    /// </summary>
    public string? FilePath { get; set; }
}
