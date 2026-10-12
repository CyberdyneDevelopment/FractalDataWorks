using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to validate syntax of code without full compilation.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ValidateSyntax")]
public sealed class ValidateSyntaxCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateSyntaxCommand"/> class.
    /// </summary>
    public ValidateSyntaxCommand()
        : base("ValidateSyntax", RoslynCommandCategories.Compilation, "Validate the syntax of a document by FilePath or an inline Code string. Use to check whether a code snippet parses before committing changes to disk; pass FilePath for an on-disk document, Code for a synthetic string. Returns a syntax-validation result with any errors.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path (optional if code provided).
    /// </summary>
    [System.ComponentModel.Description("Optional path to a source document to validate. Pass Code instead if you want to validate a synthetic snippet without writing it to disk.")]
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the C# code to validate (optional if filePath provided).
    /// </summary>
    [System.ComponentModel.Description("Optional inline source code to validate. Pass FilePath instead to validate an on-disk document.")]
    public string? Code { get; init; }
}
