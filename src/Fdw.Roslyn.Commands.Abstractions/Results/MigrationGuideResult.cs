using System;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Result of writing the migration guide markdown file.
/// </summary>
public sealed class MigrationGuideResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationGuideResult"/> class.
    /// </summary>
    public MigrationGuideResult(string outputPath, int entryCount)
    {
        OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        EntryCount = entryCount;
    }

    /// <summary>
    /// Gets the absolute path the migration guide was written to.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets the number of ledger entries included in the guide.
    /// </summary>
    public int EntryCount { get; }
}
