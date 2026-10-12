using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to extract selected code into a new method.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ExtractMethod")]
public sealed class ExtractMethodCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractMethodCommand"/> class.
    /// </summary>
    public ExtractMethodCommand()
        : base("ExtractMethod", RoslynCommandCategories.Refactoring, "Extract a selected code span into a new method on the same type. Use to break up a long method; the extracted method's parameters are inferred from variables read in the span. Returns the new method's signature and location.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start line (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Gets or sets the start column (1-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Gets or sets the end line (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Gets or sets the end column (1-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Gets or sets the name for the extracted method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;
}
