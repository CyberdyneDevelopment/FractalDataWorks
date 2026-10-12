using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to analyze coupling between types (afferent and efferent coupling).
/// </summary>
[TypeOption(typeof(RoslynCommands), "AnalyzeCoupling")]
public sealed class AnalyzeCouplingCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeCouplingCommand"/> class.
    /// </summary>
    public AnalyzeCouplingCommand()
        : base("AnalyzeCoupling", RoslynCommandCategories.Analysis, "Compute coupling metrics (afferent / efferent / instability) for the type containing the symbol at FilePath + Line + Column. Use to spot tightly coupled types that resist change — high efferent coupling means the type depends on many others; high afferent means many things depend on it. Returns the metrics plus the dependency and dependent type lists.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target type.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the target type within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the target type within FilePath.")]
    public int Column { get; init; }
}
