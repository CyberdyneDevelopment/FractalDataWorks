using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Command cannot be null.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "CommandCannotBeNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandCannotBeNullCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandCannotBeNullCode"/> class.
    /// </summary>
    public CommandCannotBeNullCode()
        : base(21000, "CommandCannotBeNull",
            ResultSeverities.ByName("Error"),
            "Command cannot be null",
            isRetryable: false)
    {
    }
}
