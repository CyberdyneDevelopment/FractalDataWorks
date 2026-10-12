using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to build GraphQL request.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "GraphQLRequestBuildFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphQLRequestBuildFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLRequestBuildFailedCode"/> class.
    /// </summary>
    public GraphQLRequestBuildFailedCode()
        : base(91001, "GraphQLRequestBuildFailed",
            ResultSeverities.ByName("Error"),
            "Failed to build GraphQL request: {ErrorMessage}",
            isRetryable: false)
    {
    }
}