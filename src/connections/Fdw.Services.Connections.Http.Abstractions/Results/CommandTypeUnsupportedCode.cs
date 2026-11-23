using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// A data command type has no entry in HttpCommandVerbs, so the protocol cannot say which HTTP method or GraphQL operation it is: the request
/// is refused where it used to be sent as POST, as a GraphQL query or with no operation name (FDW-4403).
/// </summary>
[TypeOption(typeof(HttpResultCodes), "CommandTypeUnsupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandTypeUnsupportedCode : HttpResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CommandTypeUnsupportedCode"/> class.</summary>
    public CommandTypeUnsupportedCode()
        : base(64001, "CommandTypeUnsupported",
            ResultSeverities.ByName("Error"),
            "Protocol '{Protocol}' has no HTTP verb for the data command type '{CommandType}'",
            isRetryable: false)
    {
    }
}
