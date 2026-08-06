using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// DDL command generation failed.
/// </summary>
[TypeOption(typeof(DdlResultCodes), "CommandGenerationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandGenerationFailedCode : DdlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandGenerationFailedCode"/> class.
    /// </summary>
    public CommandGenerationFailedCode()
        : base(70001, "CommandGenerationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to generate DDL commands",
            isRetryable: false)
    {
    }
}