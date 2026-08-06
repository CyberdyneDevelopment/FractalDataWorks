using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Execution context must be of the correct type.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "InvalidExecutionContext", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidExecutionContextCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidExecutionContextCode"/> class.
    /// </summary>
    public InvalidExecutionContextCode()
        : base(21001, "InvalidExecutionContext",
            ResultSeverities.ByName("Error"),
            "Execution context must be {ExpectedType}",
            isRetryable: false)
    {
    }
}
