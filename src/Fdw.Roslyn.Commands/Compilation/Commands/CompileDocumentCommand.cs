using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to compile a document and report diagnostics.
/// </summary>
[TypeOption(typeof(RoslynCommands), "CompileDocument")]
public sealed class CompileDocumentCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompileDocumentCommand"/> class.
    /// </summary>
    public CompileDocumentCommand()
        : base("CompileDocument", RoslynCommandCategories.Compilation, "Compile a single document at FilePath and report diagnostics specific to it. Use to validate a focused edit without paying the cost of a full-project build. Returns DiagnosticInfo entries for the document.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document to compile.")]
    public string FilePath { get; init; } = string.Empty;
}
