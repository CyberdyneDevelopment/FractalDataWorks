using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Command must be of the correct type for the handler.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "InvalidCommandType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidCommandTypeCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandTypeCode"/> class.
    /// </summary>
    public InvalidCommandTypeCode()
        : base(20001, "InvalidCommandType",
            ResultSeverities.ByName("Error"),
            "Command must be of type {ExpectedType}, but was {ActualType}",
            isRetryable: false)
    {
    }
}
