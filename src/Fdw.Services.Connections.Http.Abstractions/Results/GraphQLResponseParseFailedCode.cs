using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to parse GraphQL response.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "GraphQLResponseParseFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphQLResponseParseFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLResponseParseFailedCode"/> class.
    /// </summary>
    public GraphQLResponseParseFailedCode()
        : base(91002, "GraphQLResponseParseFailed",
            ResultSeverities.ByName("Error"),
            "Failed to parse GraphQL response: {ErrorMessage}",
            isRetryable: false)
    {
    }
}