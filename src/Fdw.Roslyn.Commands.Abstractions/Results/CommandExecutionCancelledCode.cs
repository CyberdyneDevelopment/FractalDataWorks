using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Command execution was cancelled.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "CommandExecutionCancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandExecutionCancelledCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionCancelledCode"/> class.
    /// </summary>
    public CommandExecutionCancelledCode()
        : base(91000, "CommandExecutionCancelled",
            ResultSeverities.ByName("Warning"),
            "Command execution was cancelled",
            isRetryable: false)
    {
    }
}
