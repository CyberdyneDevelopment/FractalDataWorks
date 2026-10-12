using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// GraphQL server returned one or more errors.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "GraphQLError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphQLErrorCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLErrorCode"/> class.
    /// </summary>
    public GraphQLErrorCode()
        : base(71001, "GraphQLError",
            ResultSeverities.ByName("Error"),
            "GraphQL error: {ErrorMessage}",
            isRetryable: false)
    {
    }
}