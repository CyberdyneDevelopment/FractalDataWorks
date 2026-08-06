using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Results;

/// <summary>
/// Command validation failed because the command type name did not match the handler's expected type.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "CommandTypeNameMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandTypeNameMismatchCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandTypeNameMismatchCode"/> class.
    /// </summary>
    public CommandTypeNameMismatchCode()
        : base(
            21000,
            "CommandTypeNameMismatch",
            ResultSeverities.ByName("Error"),
            "Command type mismatch: expected '{ExpectedType}', got '{ActualType}'",
            isRetryable: false)
    { }
}
