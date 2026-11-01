using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Command type cannot be null.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "CommandTypeCannotBeNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandTypeCannotBeNullCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandTypeCannotBeNullCode"/> class.
    /// </summary>
    public CommandTypeCannotBeNullCode()
        : base(21001, "CommandTypeCannotBeNull",
            ResultSeverities.ByName("Error"),
            "Command type cannot be null",
            isRetryable: false)
    {
    }
}
