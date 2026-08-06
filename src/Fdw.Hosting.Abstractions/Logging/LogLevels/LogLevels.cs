using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Collection of log level TypeOptions.
/// Provides type-safe access to logging levels with mappings to Serilog and Microsoft logging.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LogLevelBase), typeof(ILogLevel), typeof(LogLevels))]
public sealed partial class LogLevels : TypeCollectionBase<LogLevelBase, ILogLevel>
{
    /// <summary>
    /// Parses a log level from a string value.
    /// Supports configuration values like "Information", "Debug", etc.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The matching log level, or Information as the default.</returns>
    public static ILogLevel Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Information;

        return ByName(value!) ?? Information;
    }
}
