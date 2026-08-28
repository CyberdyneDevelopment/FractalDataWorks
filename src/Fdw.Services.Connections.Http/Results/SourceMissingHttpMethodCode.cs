using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.Results;

namespace Fdw.Services.Connections.Http.Results;

/// <summary>
/// Source configuration is missing HttpMethod — cannot resolve to a container without knowing the HTTP verb.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SourceMissingHttpMethod", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMissingHttpMethodCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMissingHttpMethodCode"/> class.
    /// </summary>
    public SourceMissingHttpMethodCode()
        : base(
            61000,
            "SourceMissingHttpMethod",
            ResultSeverities.ByName("Error"),
            "DataSet source configuration is missing HttpMethod. Set HttpMethod to the appropriate HTTP verb (GET, POST, PUT, etc.).")
    {
    }
}
