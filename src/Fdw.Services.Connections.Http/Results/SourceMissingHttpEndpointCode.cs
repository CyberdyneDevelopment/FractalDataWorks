using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.Results;

namespace Fdw.Services.Connections.Http.Results;

/// <summary>
/// Source configuration is missing HttpEndpoint — cannot resolve to a container.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SourceMissingHttpEndpoint", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMissingHttpEndpointCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMissingHttpEndpointCode"/> class.
    /// </summary>
    public SourceMissingHttpEndpointCode()
        : base(
            60000,
            "SourceMissingHttpEndpoint",
            ResultSeverities.ByName("Error"),
            "DataSet source configuration is missing HttpEndpoint. Cannot resolve HTTP source without an endpoint.")
    {
    }
}
