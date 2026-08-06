using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// POCO type has no generated mapper. Add [GenerateMapper] attribute.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "MapperNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapperNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapperNotFoundCode"/> class.
    /// </summary>
    public MapperNotFoundCode()
        : base(31008, "MapperNotFound", ResultSeverities.ByName("Error"),
            "Type '{TypeName}' has no mapper - add [GenerateMapper] attribute",
            isRetryable: false)
    {
    }
}