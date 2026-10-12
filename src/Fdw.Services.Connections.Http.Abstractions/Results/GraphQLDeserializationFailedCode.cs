using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to deserialize GraphQL data.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "GraphQLDeserializationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphQLDeserializationFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLDeserializationFailedCode"/> class.
    /// </summary>
    public GraphQLDeserializationFailedCode()
        : base(91000, "GraphQLDeserializationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to deserialize GraphQL data: {ErrorMessage}",
            isRetryable: false)
    {
    }
}