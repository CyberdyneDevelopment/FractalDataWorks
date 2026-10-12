using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to get semantic model information for a document.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetSemanticModel")]
public sealed class GetSemanticModelCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSemanticModelCommand"/> class.
    /// </summary>
    public GetSemanticModelCommand()
        : base("GetSemanticModel", RoslynCommandCategories.Compilation, "Get a summary of the semantic model for a single document: declared types, declared methods, semantic-token count. Use as a low-level check that the document parses and binds, or to enumerate top-level declarations. Returns a SemanticModel summary.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document whose semantic model is requested.")]
    public string FilePath { get; init; } = string.Empty;
}
