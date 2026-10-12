using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Orchestration.Abstractions;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Provides context information for command translation.
/// </summary>
/// <remarks>
/// Extends <see cref="IExecutionContext"/> with translation-specific metadata.
/// Universal per-run fields (ExecutionId, StartTime, CancellationToken, Logger,
/// Services, Parameters, SharedState) are inherited from <see cref="IExecutionContext"/>.
/// Translation context carries schema information and optimization hints that help
/// translators make optimal decisions during the translation process.
/// </remarks>
public interface ITranslationContext : IExecutionContext
{
    /// <summary>
    /// Gets the source schema information.
    /// </summary>
    /// <value>Schema details for the source data structure.</value>
    IDataSchema? SourceSchema { get; }

    /// <summary>
    /// Gets the target schema information.
    /// </summary>
    /// <value>Schema details for the target data structure.</value>
    IDataSchema? TargetSchema { get; }

    /// <summary>
    /// Gets translation hints for optimization.
    /// </summary>
    /// <value>Key-value pairs providing hints to the translator.</value>
    IReadOnlyDictionary<string, object> Hints { get; }

    /// <summary>
    /// Gets the maximum allowed translation time.
    /// </summary>
    /// <value>Timeout in milliseconds for translation operations.</value>
    int TranslationTimeoutMs { get; }

    /// <summary>
    /// Gets whether to prefer performance over accuracy.
    /// </summary>
    /// <value>True to optimize for speed rather than perfect translation.</value>
    bool PreferPerformance { get; }

    /// <summary>
    /// Gets whether to include debugging information.
    /// </summary>
    /// <value>True to include additional debugging metadata in the result.</value>
    bool IncludeDebugInfo { get; }

    /// <summary>
    /// Gets the target environment for the translated command.
    /// </summary>
    /// <value>Information about where the command will execute.</value>
    string? TargetEnvironment { get; }
}