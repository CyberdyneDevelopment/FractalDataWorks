using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Unexpected error during HTTP operation.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "UnexpectedError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnexpectedErrorCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedErrorCode"/> class.
    /// </summary>
    public UnexpectedErrorCode()
        : base(90000, "UnexpectedError",
            ResultSeverities.ByName("Error"),
            "Unexpected error: {ErrorMessage}",
            isRetryable: false)
    {
    }
}