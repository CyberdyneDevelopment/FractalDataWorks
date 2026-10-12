using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// The protocol cannot express one of the filter's operators.
/// </summary>
/// <remarks>
/// Shared across every HTTP protocol (Rest, JsonApi, OData, GraphQL, ...), not just the
/// query-string-based ones -- the message deliberately says nothing about where the filter would
/// have gone (querystring vs. request body), since that differs per protocol.
/// </remarks>
[TypeOption(typeof(HttpResultCodes), "UnsupportedFilterOperator", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnsupportedFilterOperatorCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedFilterOperatorCode"/> class.
    /// </summary>
    public UnsupportedFilterOperatorCode()
        : base(91013, "UnsupportedFilterOperator",
            ResultSeverities.ByName("Error"),
            "Protocol '{Protocol}' cannot express filter operator '{Operator}' on '{Field}'",
            isRetryable: false)
    {
    }
}
