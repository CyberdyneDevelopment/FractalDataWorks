using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Command execution failed.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "CommandExecutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandExecutionFailedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionFailedCode"/> class.
    /// </summary>
    public CommandExecutionFailedCode()
        : base(91001, "CommandExecutionFailed",
            ResultSeverities.ByName("Error"),
            "Command execution failed: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
