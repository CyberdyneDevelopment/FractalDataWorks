using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to get syntax tree information for a document.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetSyntaxTree")]
public sealed class GetSyntaxTreeCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSyntaxTreeCommand"/> class.
    /// </summary>
    public GetSyntaxTreeCommand()
        : base("GetSyntaxTree", RoslynCommandCategories.Compilation, "Return the syntax tree for the document at FilePath in structured form. Set IncludeTrivia=true to include comments and whitespace. Use for direct AST inspection — most callers should prefer higher-level commands. Returns a SyntaxNode tree.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document whose syntax tree is requested.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include trivia in output.
    /// </summary>
    [System.ComponentModel.Description("When true, include comments and whitespace in the syntax tree output; defaults to false for compactness.")]
    public bool IncludeTrivia { get; init; }
}
