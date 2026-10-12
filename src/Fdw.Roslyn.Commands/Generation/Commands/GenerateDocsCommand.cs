using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate XML documentation for code members.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateDocs")]
public sealed class GenerateDocsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateDocsCommand"/> class.
    /// </summary>
    public GenerateDocsCommand()
        : base("GenerateDocs", RoslynCommandCategories.Generation, "Generate XML documentation comments for public APIs in a document (class, method, property summaries inferred from signatures and naming). Use as a starting point before hand-writing the real docs; descriptions are heuristic. Returns the count of documented members.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether to document private members.
    /// </summary>
    public bool IncludePrivate { get; set; }
}
