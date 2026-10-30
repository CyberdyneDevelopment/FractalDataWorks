using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// An OpenAPI field declares no type, or a type no JSON Schema converter maps.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiFieldTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiFieldTypeUnknownCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiFieldTypeUnknownCode"/> class.
    /// </summary>
    public OpenApiFieldTypeUnknownCode()
        : base(21002, "OpenApiFieldTypeUnknown",
            ResultSeverities.ByName("Error"),
            "Field '{Field}' in {Location} has source type '{SourceType}', which no JSON Schema converter maps",
            isRetryable: false)
    {
    }
}