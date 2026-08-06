using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// No matching OpenAPI operation found for the command.
/// </summary>
[TypeOption(typeof(OpenApiResultCodes), "NoMatchingOperation", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoMatchingOperationCode : OpenApiResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public NoMatchingOperationCode()
        : base(30000, "NoMatchingOperation",
            ResultSeverities.ByName("Error"),
            "No matching OpenAPI operation found for {CommandType} on '{ResourceName}'",
            isRetryable: false)
    {
    }
}