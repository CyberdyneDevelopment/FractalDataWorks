using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to get detailed symbol information.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetSymbolInfo")]
public sealed class GetSymbolInfoCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSymbolInfoCommand"/> class.
    /// </summary>
    public GetSymbolInfoCommand()
        : base("GetSymbolInfo", RoslynCommandCategories.Analysis, "Resolve the symbol at FilePath + Line + Column and return rich metadata: kind, accessibility, modifiers, signature, containing type/namespace, locations, and attribute list. Use as the first step in any 'tell me about this symbol' workflow. Returns a SymbolInfoResult object.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target symbol.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the target symbol within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the target symbol within FilePath.")]
    public int Column { get; init; }
}
