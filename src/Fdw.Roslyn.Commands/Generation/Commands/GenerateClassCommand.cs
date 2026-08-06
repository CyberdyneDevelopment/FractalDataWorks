using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate a class from a template.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateClass")]
public sealed class GenerateClassCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateClassCommand"/> class.
    /// </summary>
    public GenerateClassCommand()
        : base("GenerateClass", RoslynCommandCategories.Generation, "Generate a new class from a template — name, namespace, base type, implemented interfaces, modifiers. Use to scaffold a new type with the project's conventions applied (Directory.Build.props inheritance, MinVer stamping, analyzer config). Returns the file path of the new class.")
    {
    }
    /// <summary>
    /// Gets or sets the name of the class.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace for the class.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base class to inherit from.
    /// </summary>
    public string? BaseClass { get; set; }

    /// <summary>
    /// Gets or sets comma-separated list of interfaces.
    /// </summary>
    public string? Interfaces { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the class is sealed.
    /// </summary>
    public bool IsSealed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the class is abstract.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Gets or sets the project name where the class should be added.
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the file path where the class should be created.
    /// </summary>
    public string? FilePath { get; set; }
}
