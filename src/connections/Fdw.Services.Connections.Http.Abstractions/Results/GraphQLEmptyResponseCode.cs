using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Empty GraphQL response received.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "GraphQLEmptyResponse", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphQLEmptyResponseCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLEmptyResponseCode"/> class.
    /// </summary>
    public GraphQLEmptyResponseCode()
        : base(71000, "GraphQLEmptyResponse",
            ResultSeverities.ByName("Error"),
            "Empty GraphQL response",
            isRetryable: false)
    {
    }
}