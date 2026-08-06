using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// GraphQL HTTP error response.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "GraphQLHttpError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphQLHttpErrorCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLHttpErrorCode"/> class.
    /// </summary>
    public GraphQLHttpErrorCode()
        : base(71002, "GraphQLHttpError",
            ResultSeverities.ByName("Error"),
            "HTTP {StatusCode}: {ReasonPhrase}",
            isRetryable: true)
    {
    }
}