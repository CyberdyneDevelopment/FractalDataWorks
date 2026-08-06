using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Results;

/// <summary>
/// Command validation failed due to type mismatch.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "CommandTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandTypeMismatchCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandTypeMismatchCode"/> class.
    /// </summary>
    public CommandTypeMismatchCode()
        : base(
            20001,
            "CommandTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Command must be of type {ExpectedType}, but was {ActualType}",
            isRetryable: false)
    { }
}
